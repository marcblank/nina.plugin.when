#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Validations;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Sequencer.SequenceItem;
using NINA.Astrometry;
using NINA.Profile;
using NINA.Profile.Interfaces;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Cool Camera +")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Camera_CoolCamera_Description")]
    [ExportMetadata("Icon", "SnowflakeSVG")]
    [ExportMetadata("Category", "Powerups (Enhanced Instructions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class CoolCamera : SequenceItem, IValidatable {

        IProfileService profileService;

        [ImportingConstructor]
        public CoolCamera(IProfileService profileService, ICameraMediator cameraMediator) {
            this.cameraMediator = cameraMediator;
            this.profileService = profileService;
            CameraSettings = profileService.ActiveProfile.CameraSettings;
        }

        private CoolCamera(CoolCamera cloneMe) : this(cloneMe.profileService, cloneMe.cameraMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new CoolCamera(this) {
                Temperature = Temperature,
                TemperatureExpr = TemperatureExpr,
                Duration = Duration,
                DurationExpr = DurationExpr
            };
        }

        private ICameraMediator cameraMediator;

        private ICameraSettings cameraSettings;

        public ICameraSettings CameraSettings {
            get {
                if (cameraSettings.Temperature == null) {
                    cameraSettings.Temperature = 0;
                }
                return cameraSettings;
                    }
            private set {
                cameraSettings = value;
                RaisePropertyChanged();
            }
        }

        private double temperature = 0;

        // COMMENTED OUT FOR CONSTANTS SUPPORT
        //[JsonProperty]
        public double Temperature {
            get =>  temperature;
            set {
                temperature = value;
                RaisePropertyChanged();
            }
        }

        // *** ADDED FOR CONSTANTS SUPPORT ***
        private string temperatureExpr = "";
        
        [JsonProperty]
        public string TemperatureExpr {
            get => temperatureExpr;
            set {
                temperatureExpr = value;
                ConstantExpression.Evaluate(this, "TemperatureExpr", "Temperature", CameraSettings.Temperature);
                RaisePropertyChanged("TemperatureExpr");
            }
        }
        // *** ADDED FOR CONSTANTS SUPPORT ***


        private double duration = 0;

        public double Duration {
            get => duration;
            set {
                duration = value;
                RaisePropertyChanged();
            }
        }

        private string durationExpr = "0";

        [JsonProperty]
        public string DurationExpr {
            get => durationExpr;
            set {
                durationExpr = value;
                ConstantExpression.Evaluate(this, "DurationExpr", "Duration", 0);
                RaisePropertyChanged("DurationExpr");
            }
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return cameraMediator.CoolCamera(Temperature, TimeSpan.FromMinutes(Duration), progress, token);
        }

        private static string BAD_TEMPERATURE = "Temperature must be between -30C and 30C";
        
        public bool Validate() {
            var i = new List<string>();
            var info = cameraMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            } else if (!info.CanSetTemperature) {
                //i.Add(Loc.Instance["Lbl_SequenceItem_Validation_CameraCannotSetTemperature"]);
            }


            CameraSettings = profileService.ActiveProfile.CameraSettings;
            ConstantExpression.Evaluate(this, "TemperatureExpr", "Temperature", CameraSettings.Temperature, i);
            ConstantExpression.Evaluate(this, "DurationExpr", "Duration", 0, i);

            if (ValidateTemperature(temperature) != String.Empty) {
                i.Add(BAD_TEMPERATURE);
            }

            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public string ValidateTemperature(double temp) {
                if (temp < -30 || temp > 30) {
                return BAD_TEMPERATURE;
            }
            return string.Empty;
        }

        public override TimeSpan GetEstimatedDuration() {
            return Duration > 0 ? TimeSpan.FromMinutes(Duration) : TimeSpan.FromMinutes(1);
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(CoolCamera)}, Temperature: {Temperature}, Duration: {Duration}";
        }
    }
}