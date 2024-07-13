#region "copyright"

/*
    Copyright © 2021 Francesco Meschia <francesco.meschia@gmail.com>
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.Sequencer.SequenceItem;
using NINA.Astrometry;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.MeridianFlip;
using NINA.Core.Enum;
using NINA.Profile.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using NINA.Sequencer.Container;
using NINA.Core.Model;
using NINA.Sequencer.Utility;
using System.Threading.Tasks;
using System.Threading;
using NINA.WPF.Base.Interfaces;
using NINA.Profile;
using Accord.Imaging.Filters;
using Accord.Statistics.Kernels;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Windows.Media;

namespace fmeschia.NINA.Plugin.SmartMeridianFlip.Sequencer.Trigger
{

    /// <summary>
    /// This trigger controls the meridian flip for German equatorial ounts in a more flexible way than N.I.N.A.'s default
    /// meridian flip trigger. Instead of the three settings ("Pause Before Meridian", "Minutes After Meridian", and "Max. Minutes
    /// After Meridian") of the basic meridian flip trigger, the plug-in that makes this trigger available can be configured
    /// with an "obstruction file", that describes the safe limits that the scope must observe while on the West and East sides 
    /// of the pier, as a function of the target's declination. In this way, imaging time can be optimized based on the target's
    /// declination.
    /// </summary>
    [ExportMetadata("Name", "Smart Meridian Flip")]
    [ExportMetadata("Description", "A trigger to initiate a meridian flip that can be configured to avoid obstructions (e.g. tripod crashes)")]
    [ExportMetadata("Icon", "SmartMeridianFlipTriggerSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SmartMeridianFlipTrigger : MeridianFlipTrigger {

        [ImportingConstructor]
        public SmartMeridianFlipTrigger(IProfileService profileService, ICameraMediator cameraMediator, ITelescopeMediator telescopeMediator, IFocuserMediator focuserMediator, IApplicationStatusMediator applicationStatusMediator, IMeridianFlipVMFactory meridianFlipVMFactory) : base(profileService, cameraMediator, telescopeMediator, focuserMediator, applicationStatusMediator, meridianFlipVMFactory) {
        }

        public SmartMeridianFlipTrigger(SmartMeridianFlipTrigger copyMe): this(copyMe.profileService, copyMe.cameraMediator, copyMe.telescopeMediator, copyMe.focuserMediator, copyMe.applicationStatusMediator, copyMe.meridianFlipVMFactory) {
            CopyMetaData(copyMe);
        }

        public struct TimeToEvent
        {
            public TimeToEvent(TimeSpan time, bool abovePole) {
                Time = time;
                AbovePole = abovePole;
            }
            public TimeSpan Time;
            public bool AbovePole;
            public override string ToString() {
                return $"TimeToEvent({Time}, {AbovePole})";
            }
        } 


        protected TimeSpan TimeToMeridian(Coordinates coordinates, double localSiderealTime) {
            // calculates how long before the object crosses the meridian above the pole
            coordinates = coordinates.Transform(Epoch.JNOW);
            var hoursToMeridian = (- (localSiderealTime - coordinates.RA) + 36) % 24 - 12;
            // if we already performed a flip at the target's coordinates in the last 12 hours, it means we need to be looking at the next meridian crossing
            if (hoursToMeridian < 0 && (DateTime.UtcNow - lastFlipTime) < TimeSpan.FromHours(12) && lastFlipCoordiantes != null 
                && (lastFlipCoordiantes - GetTargetCoordinates(this.Parent)).Distance.ArcMinutes < 20) { 
                hoursToMeridian += 24; 
            }
            if (hoursToMeridian < -6) {
                hoursToMeridian += 24;
            }
            return TimeSpan.FromHours(hoursToMeridian);
        }

        public override double TimeToMeridianFlip {
            get {
                var telescopeInfo = telescopeMediator.GetInfo();
                var localSiderealTime = telescopeInfo.SiderealTime;
                var target = GetTargetCoordinates(this.Parent);
                return TimeToMeridianFlipInternal(target, localSiderealTime).Time.TotalHours;
            }
            set { }
        }

        protected TimeToEvent TimeToMeridianFlipInternal(Coordinates target, double localSiderealTime) {
            // this function finds the next meridian flip, by determining the upcoming one
            // (it could be that the scope crossed the meridian but hasn't flipped yet, or the one
            // for the next meridian crossing). Returns the time of the next flip, as well as whether it
            // will occur "above the Pole" or "below the Pole"
            target = target.Transform(Epoch.JNOW);
            var mpha_above = MaximumProximalHourAngle(target, true);
            var mdha_above = MinimumDistalHourAngle(target, true);
            var mpha_below = MaximumProximalHourAngle(target, false);
            var mdha_below = MinimumDistalHourAngle(target, false);
            // projected sidereal time for the next flip above the pole
            var projectedSiderealTimeAbove = AstroUtil.EuclidianModulus(localSiderealTime -
                Math.Max(1/60d, Math.Max(-mpha_above.TotalHours, mdha_above.TotalHours)), 24);
            // projected sidereal time for the next flip below the pole
            var projectedSiderealTimeBelow = AstroUtil.EuclidianModulus(localSiderealTime + 12 -
                Math.Max(1/60d, Math.Max(-mpha_below.TotalHours, mdha_below.TotalHours)), 24);

            // (in both these calculations, it is assumed that the earliest possible flip happens one mninute past meridian,
            // so that it would (hopefully) work with all mounts)

            // calculates how long before each of the two events
            var timeToMeridianFlipAbove = TimeToMeridian(target, localSiderealTime: projectedSiderealTimeAbove);
            Logger.Debug($"Time to meridian flip above the pole: {timeToMeridianFlipAbove}");
            /*var timeToMeridianFlipBelow = TimeSpan.FromHours(AstroUtil.EuclidianModulus(
                TimeToMeridian(target, localSiderealTime: projectedSiderealTimeBelow).TotalHours,
                24));*/
            var timeToMeridianFlipBelow = TimeToMeridian(target, localSiderealTime: projectedSiderealTimeBelow);
            Logger.Debug($"Time to meridian flip below the pole: {timeToMeridianFlipBelow}");
            // var timeToMeridian = TimeSpan.FromHours((-(localSiderealTime - target.RA) + 36 )% 24 - 12) +
            //     TimeSpan.FromHours(timeToMeridianFlipAbove < timeToMeridianFlipBelow ? 0 : 12);
            // this is the upcoming one
            var timeToEvent = Math.Min(timeToMeridianFlipAbove.TotalHours, timeToMeridianFlipBelow.TotalHours);
            Logger.Debug($"Time to next meridian flip: {TimeSpan.FromHours(timeToEvent)}");
            return new TimeToEvent(TimeSpan.FromHours(timeToEvent), timeToMeridianFlipAbove < timeToMeridianFlipBelow);
        }

        protected TimeToEvent TimeToStopTracking(Coordinates target, double localSiderealTime) {
            target = target.Transform(Epoch.JNOW);
            var timeToMeridianFlip = TimeToMeridianFlipInternal(target, localSiderealTime);
            var timeToMeridian = (-(localSiderealTime - target.RA) + (timeToMeridianFlip.AbovePole ? 0 : 12) + 36) % 24 - 12;
            // the time-to-meridian is calculated from the meridian crossing time, referring to the same crossing
            // (above/below pole) as the one that timeToMeridianFlip refers to, by subtracting from it the maximum
            // proximal hour angle
            var mpha = MaximumProximalHourAngle(target, timeToMeridianFlip.AbovePole);
            var timeToStop = timeToMeridian - mpha.TotalHours;
            Logger.Debug($"Time to stop tracking is {TimeSpan.FromHours(timeToStop)}");
            return new TimeToEvent(TimeSpan.FromHours(timeToStop), timeToMeridianFlip.AbovePole);
        }

        protected TimeSpan MaximumProximalHourAngle(Coordinates target, bool above) {
            // The maximum proximal hour angle is the limit angle that can be reached while tracking, without having
            // any collision. It represents the "West limit" when crossing the arc of meridian above the pole, and the
            // "East limit" when crossing the arc of meridian below the pole.
            // Its value (in hours) is positive if the angle is to be intended before reaching the meridian, and negative 
            // if past the meridian.
            target = target.Transform(Epoch.JNOW);
            double hourAngle;
            if (above) {
                hourAngle = SmartMeridianFlipMediator.Instance.Plugin.obstructions.GetWestObstruction(target.Dec);
            } else {
                hourAngle = SmartMeridianFlipMediator.Instance.Plugin.obstructions.GetEastObstruction(180 - target.Dec);
            }
            Logger.Trace($"Maxumum Proximal Hour Angle for target {target}, aboveThePole={above} : {TimeSpan.FromHours(hourAngle)}");
            return TimeSpan.FromHours(hourAngle / 60d);
        }

        protected TimeSpan MinimumDistalHourAngle(Coordinates target, bool above) {
            // The minimum distal hour angle is the minimum angle past the meridian where the scope can be flipped
            // without having a collision. It represents the "Eest limit" when crossing the arc of meridian above
            // the pole, and the "Wast limit" when crossing the arc of meridian below the pole.
            // Its value (in hours) is positive if the angle is past the meridian, and negative if before the meridian.
            target = target.Transform(Epoch.JNOW);
            double hourAngle;
            if (above) {
                hourAngle = SmartMeridianFlipMediator.Instance.Plugin.obstructions.GetEastObstruction(target.Dec);
            } else {
                hourAngle = SmartMeridianFlipMediator.Instance.Plugin.obstructions.GetWestObstruction(180 - target.Dec);
            }
            Logger.Trace($"Minimum Distal Hour Angle for target {target}, aboveThePole={above} : {TimeSpan.FromHours(hourAngle)}");
            return TimeSpan.FromHours(hourAngle / 60d);
        }

        // The UI widget of the Smart Meridian Flip trigger in the Sequencer shows two time values: the time when tracking
        // stops, and the time when the meridian flip actually happens. This is different from the default meridian flip trigger,
        // which shows the limits of the "meridian flip zone" (time between Minutes After Meridian and Max. Minutes After
        // Meridian) when Pause Before Meridian is not set, and shows the time when tracking stops (in both values) when Pause
        // Before Meridian is used.
        // For compatibility, SMF will set the two ihnerited properties of LatestFlipTime and EarliestFlipTime to be
        // both equal to the time when tracking stops, and will use a new property (ActualFlipTime) to show the time when the
        // meridian flip actually happens.

        public override DateTime LatestFlipTime {
            get => latestFlipTime;
            protected set {
                latestFlipTime = value;
                RaisePropertyChanged();
            }
        }
        
        public override DateTime EarliestFlipTime {
            get => earliestFlipTime;
            protected set {
                earliestFlipTime = value;
                RaisePropertyChanged();
            }
        }
        
        private DateTime actualFlipTime;

        public virtual DateTime ActualFlipTime {
            get => actualFlipTime;
            protected set {
                actualFlipTime = value;
                RaisePropertyChanged();
            }
        }

        public TimeSpan TimeRemaining(Coordinates target) {
            target = target.Transform(Epoch.JNOW);
            var localSideralTime = telescopeMediator.GetInfo().SiderealTime;
            var timeToFlip = TimeToMeridianFlipInternal(target, localSideralTime);
            //Logger.Debug($"Time to next meridian flip is {timeToFlip.Time}");
            // If we're here, it means we are approaching (but likely not crossed the proximal limit). So there will liley be a certain wait.
            // Let's see if we have cleared the distal limit - if so, we may be able to speed it up
            var mdha = MinimumDistalHourAngle(target, timeToFlip.AbovePole);
            Logger.Debug($"Minimum distal hour angle is {mdha}");
            var ha = localSideralTime + (timeToFlip.AbovePole ? 0 : 12) - target.RA + 36 % 24 - 12;
            Logger.Debug($"HA of the target right now is {TimeSpan.FromHours(ha)}");
            // We wait from now to until we have cleared the minimum distal hour angle or 1 minute past the meridian
            var timeRemaining = TimeSpan.FromHours(Math.Max(0, Math.Max(1d / 60, mdha.TotalHours) - ha));
            if (timeToFlip.Time.TotalHours > 3) {
                //Assume a delayed flip when the time is more than two hours and flip immediately
                Logger.Info("Looks like meridian flip has been delayed -- we should flip now");
                timeToFlip = new TimeToEvent(TimeSpan.Zero, timeToFlip.AbovePole);
            }
            timeToFlip.Time = timeRemaining;
            if (timeToFlip.Time == TimeSpan.Zero) {
                Logger.Info("No need to further wait, flipping NOW");
            } else {
                Logger.Info($"Waiting for {timeToFlip.Time} before flipping");
            }
            return timeToFlip.Time;
        }

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            Coordinates target = GetTargetCoordinates(context);
            target = target.Transform(Epoch.JNOW);
            var delayBeforeFlip = TimeRemaining(target);
            lastFlipTime = DateTime.UtcNow;
            lastFlipCoordiantes = target;
            return meridianFlipVMFactory.Create().MeridianFlip(target, delayBeforeFlip, token);
        }

        private Coordinates GetTargetCoordinates(ISequenceContainer context) {
            var contextCoordinates = ItemUtility.RetrieveContextCoordinates(context);
            Coordinates target;
            if (contextCoordinates != null) {
                target = contextCoordinates.Coordinates;
            } else {
                target = telescopeMediator.GetCurrentPosition();
                Logger.Warning("No target information available to evaluate flip. Taking current telescope coordinates instead");
            }
            return target;
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            var telescopeInfo = telescopeMediator.GetInfo();

            if (!telescopeInfo.Connected || double.IsNaN(telescopeInfo.TimeToMeridianFlip)) {
                Logger.Error("Smart Meridian Flip - Telescope is not connected to evaluate if a flip should happen!");
                return false;
            }

            if (!telescopeInfo.TrackingEnabled) {
                Logger.Info("Smart Meridian Flip - Telescope is not tracking. Skip flip evaluation");
                return false;
            }

            var localSiderealTime = telescopeMediator.GetInfo().SiderealTime;
            var target = GetTargetCoordinates(this.Parent);

            var nextInstructionTime = nextItem?.GetEstimatedDuration().TotalSeconds ?? 0;
            var timeToMeridianFlip = TimeToMeridianFlipInternal(target, localSiderealTime);
            var timeToStopTracking = TimeToStopTracking(target, localSiderealTime);
            //var timeToThisMeridian = (-(localSiderealTime - target.RA) + 36) % 24 - 12 + (timeToMeridianFlip.AbovePole ? 0 : 12);
            //var timeToProximalCriticalBoundary = TimeSpan.FromHours(Math.Min(timeToThisMeridian, Math.Min(timeToMeridianFlip.Time.TotalHours, timeToStopTracking.Time.TotalHours)));
            //var timeToDistalCriticalBoundary = TimeSpan.FromHours(Math.Max(timeToThisMeridian, Math.Max(timeToMeridianFlip.Time.TotalHours, timeToStopTracking.Time.TotalHours)));
            var timeToFlip = timeToMeridianFlip.Time;
            var timeToStop = timeToStopTracking.Time;
            
            bool needsFlip = false;
            bool skipTheRest = false;
            // if (timeToProximalCriticalBoundary <= TimeSpan.FromSeconds(nextInstructionTime) && timeToDistalCriticalBoundary > TimeSpan.Zero) {
            if (UseSideOfPier) {
                if (telescopeInfo.SideOfPier != PierSide.pierUnknown) {
                    var projectedSiderealTime = Angle.ByHours(AstroUtil.EuclidianModulus(telescopeInfo.SiderealTime + timeToMeridianFlip.Time.TotalHours + 1 / 60.0, 24));
                    var targetSideOfPier = MeridianFlip.ExpectedPierSide(
                        coordinates: telescopeInfo.Coordinates,
                        localSiderealTime: projectedSiderealTime);
                    if (telescopeInfo.SideOfPier == targetSideOfPier) {
                        Logger.Info($"Smart Meridian Flip - Telescope already reports {telescopeInfo.SideOfPier}. Automated Flip will not be performed.");
                        if (!((DateTime.UtcNow - lastFlipTime) < TimeSpan.FromHours(12) && (lastFlipCoordiantes - target).Distance.ArcMinutes < 20)) {
                            lastFlipTime = DateTime.UtcNow + timeToMeridianFlip.Time;
                            lastFlipCoordiantes = target;
                        }
                        skipTheRest = true;
                    }
                }
            } else if ((DateTime.UtcNow - lastFlipTime) < TimeSpan.FromHours(11) && lastFlipCoordiantes != null && (lastFlipCoordiantes - telescopeInfo.Coordinates).Distance.ArcMinutes < 20) {
                skipTheRest = true;
            }
            //}

            if (!skipTheRest && timeToStopTracking.Time <= TimeSpan.FromSeconds(nextInstructionTime)) {
                Logger.Info("Smart Meridian Flip - West limit passed - stopping tracking and going to into flip wait routine");
                needsFlip = true;
            }

            UpdateMeridianFlipTimeTriggerValues();

            return needsFlip;
        }

        protected virtual void UpdateMeridianFlipTimeTriggerValues() {
            //Update the FlipTimes
            var telescopeInfo = telescopeMediator.GetInfo();
            var localSiderealTime = telescopeInfo.SiderealTime;
            Coordinates target = GetTargetCoordinates(this.Parent);
            EarliestFlipTime = (DateTime.UtcNow + TimeToStopTracking(target, localSiderealTime).Time).ToLocalTime();
            LatestFlipTime = earliestFlipTime;
            //ActualFlipTime = (DateTime.UtcNow + TimeRemaining(target, localSiderealTime)).ToLocalTime();
            ActualFlipTime = (DateTime.UtcNow + TimeToMeridianFlipInternal(target, localSiderealTime).Time).ToLocalTime();
            Logger.Info($"Smart Meridian Flip - Scope will stop tracking at {EarliestFlipTime} and will flip at {ActualFlipTime}");
        }

        public override object Clone() {
            return new SmartMeridianFlipTrigger(this);
        }

        public override string ToString() {
            return $"Trigger: {nameof(SmartMeridianFlipTrigger)}";
        }

    }
}