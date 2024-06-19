﻿#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.Validations;
using NINA.Equipment.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Sequencer.Utility;
using NINA.Image.ImageAnalysis;
using NINA.Sequencer.Interfaces;
using NINA.WPF.Base.Interfaces;
using NINA.Sequencer.Trigger;
using System.Windows;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.WPF.Base.Utility.AutoFocus;
using NINA.Core.Utility.Notification;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "AF After #Exposures +")]
    [ExportMetadata("Description", "Lbl_SequenceTrigger_AutofocusAfterExposures_Description")]
    [ExportMetadata("Icon", "AutoFocusAfterExposuresSVG")]
    [ExportMetadata("Category", "Powerups (Test)")]
    //[Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class AutofocusAfterExposures : SequenceTrigger, IValidatable {
        private IProfileService profileService;

        private IImageHistoryVM history;
        private ICameraMediator cameraMediator;
        private IFilterWheelMediator filterWheelMediator;
        private IFocuserMediator focuserMediator;
        private IAutoFocusVMFactory autoFocusVMFactory;

        private int afterExposures;

        [ImportingConstructor]
        public AutofocusAfterExposures(IProfileService profileService, IImageHistoryVM history, ICameraMediator cameraMediator, IFilterWheelMediator filterWheelMediator, IFocuserMediator focuserMediator, IAutoFocusVMFactory autoFocusVMFactory) : base() {
            this.history = history;
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.focuserMediator = focuserMediator;
            this.autoFocusVMFactory = autoFocusVMFactory;
            AfterExposures = 5;
            TriggerRunner.Add(new Annotation());
        }

        private AutofocusAfterExposures(AutofocusAfterExposures cloneMe) : this(cloneMe.profileService, cloneMe.history, cloneMe.cameraMediator, cloneMe.filterWheelMediator, cloneMe.focuserMediator, cloneMe.autoFocusVMFactory) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new AutofocusAfterExposures(this) {
                AfterExposures = AfterExposures,
                TriggerRunner = (SequentialContainer)TriggerRunner.Clone()
            };
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = ImmutableList.CreateRange(value);
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public int AfterExposures {
            get => afterExposures;
            set {
                afterExposures = value;
                RaisePropertyChanged();
            }
        }

        public int ProgressExposures { get; private set; }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            AutoFocusReport report = new AutoFocusReport() {
                Timestamp = DateTime.Now,
                StarDetectorName = "Sim",
                AutoFocuserName = "Sim",
                Duration = TimeSpan.FromSeconds(60),
            };
            history.AppendAutoFocusPoint(report);
            Notification.ShowInformation("Test Autofocus Run completed");
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            Logger.Warning("ST");
            if (nextItem == null) {
                Logger.Warning("ST: nextItem null");
                return false;
            }
            if (!(nextItem is IExposureItem exposureItem)) {
                Logger.Warning("ST: Not exposure item");
                return false; 
            }
            if (exposureItem.ImageType != "LIGHT") {
                Logger.Warning("ST: Isn't LIGHT");
                return false; 
            }

            var lastAFId = history.AutoFocusPoints?.LastOrDefault()?.Id ?? 0;
            var lightImageHistory = history.ImageHistory.Where(x => x.Type == "LIGHT" && x.Id > lastAFId).ToList();
            ProgressExposures = lightImageHistory.Count % AfterExposures;
            Logger.Warning("ProgressExposures = " + ProgressExposures + ", lastAFId = " + lastAFId + ", Count = " + lightImageHistory.Count);
            RaisePropertyChanged(nameof(ProgressExposures));


            var shouldTrigger =
                lastAFId < history.ImageHistory.Count
                && history.ImageHistory.Count > 0
                && ProgressExposures == 0;

            Logger.Warning("ST: " + shouldTrigger);

            if (shouldTrigger) {
                if (ItemUtility.IsTooCloseToMeridianFlip(Parent, TriggerRunner.GetItemsSnapshot().First().GetEstimatedDuration() + nextItem?.GetEstimatedDuration() ?? TimeSpan.Zero)) {
                    Logger.Warning("Autofocus should be triggered, however the meridian flip is too close to be executed");
                    shouldTrigger = false;
                }
            }

            return shouldTrigger;
        }

        public override string ToString() {
            return $"Trigger: {nameof(AutofocusAfterExposures)}, AfterExposures: {AfterExposures}";
        }

        public bool Validate() {
            var i = new List<string>();
            var cameraInfo = cameraMediator.GetInfo();
            var focuserInfo = focuserMediator.GetInfo();

            if (!cameraInfo.Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            }
            if (!focuserInfo.Connected) {
                i.Add(Loc.Instance["LblFocuserNotConnected"]);
            }

            Issues = i;
            return i.Count == 0;
        }
    }
}