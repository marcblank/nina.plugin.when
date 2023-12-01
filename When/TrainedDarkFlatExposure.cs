﻿#region "copyright"

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
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.Trigger;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.Model;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Locale;
using NINA.Profile;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Sequencer.Utility;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using WhenPlugin.When;
using NINA.Sequencer.SequenceItem.FlatDevice;
using NINA.Sequencer.SequenceItem;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Trained Dark Flat Exposure +")]
    [ExportMetadata("Description", "Lbl_SequenceItem_FlatDevice_TrainedDarkFlatExposure_Description")]
    [ExportMetadata("Icon", "BrainBulbSVG")]
    [ExportMetadata("Category", "Powerups (Enhanced Instructions)")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TrainedDarkFlatExposure : SequentialContainer, IImmutableContainer {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.Items.Clear();
            this.Conditions.Clear();
            this.Triggers.Clear();
        }

        private IProfileService profileService;
        private ICameraMediator cameraMediator;
        private bool keepPanelClosed;

        [ImportingConstructor]
        public TrainedDarkFlatExposure(IProfileService profileService, ICameraMediator cameraMediator, IImagingMediator imagingMediator, IImageSaveMediator imageSaveMediator, IImageHistoryVM imageHistoryVM, IFilterWheelMediator filterWheelMediator, IFlatDeviceMediator flatDeviceMediator) :
            this(
                null,
                profileService,
                cameraMediator,
                new CloseCover(flatDeviceMediator),
                new ToggleLight(flatDeviceMediator) { OnOff = false },
                new SwitchFilter(profileService, filterWheelMediator),
                new SetBrightness(flatDeviceMediator),
                new TakeExposure(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM) { ImageType = CaptureSequence.ImageTypes.DARKFLAT },
                new LoopCondition() { Iterations = 1 },
                new OpenCover(flatDeviceMediator)

            ) {
        }

        private TrainedDarkFlatExposure(
            TrainedDarkFlatExposure cloneMe,
            IProfileService profileService,
            ICameraMediator cameraMediator,
            CloseCover closeCover,
            ToggleLight toggleLightOff,
            SwitchFilter switchFilter,
            SetBrightness setBrightness,
            TakeExposure takeExposure,
            LoopCondition loopCondition,
            OpenCover openCover
            ) {
            this.profileService = profileService;

            this.Add(closeCover);
            this.Add(toggleLightOff);
            this.Add(switchFilter);
            this.Add(setBrightness);

            var container = new SequentialContainer();
            container.Add(loopCondition);
            container.Add(takeExposure);
            this.Add(container);
            this.Add(openCover);

            IsExpanded = false;

            if (cloneMe != null) {
                CopyMetaData(cloneMe);
            }
        }

        private InstructionErrorBehavior errorBehavior = InstructionErrorBehavior.ContinueOnError;

        [JsonProperty]
        public override InstructionErrorBehavior ErrorBehavior {
            get => errorBehavior;
            set {
                errorBehavior = value;
                foreach (var item in Items) {
                    item.ErrorBehavior = errorBehavior;
                }
                RaisePropertyChanged();
            }
        }

        private int attempts = 1;

        [JsonProperty]
        public override int Attempts {
            get => attempts;
            set {
                if (value > 0) {
                    attempts = value;
                    foreach (var item in Items) {
                        item.Attempts = attempts;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        [JsonProperty]
        public bool KeepPanelClosed {
            get => keepPanelClosed;
            set {
                keepPanelClosed = value;

                RaisePropertyChanged();
            }
        }

        public CloseCover GetCloseCoverItem() {
            return (Items[0] as CloseCover);
        }

        public ToggleLight GetToggleLightOffItem() {
            return (Items[1] as ToggleLight);
        }

        public SwitchFilter GetSwitchFilterItem() {
            return (Items[2] as SwitchFilter);
        }

        public SetBrightness GetSetBrightnessItem() {
            return (Items[3] as SetBrightness);
        }

        public SequentialContainer GetImagingContainer() {
            return (Items[4] as SequentialContainer);
        }

        public TakeExposure GetExposureItem() {
            return ((Items[4] as SequentialContainer).Items[0] as TakeExposure);
        }

        public LoopCondition GetIterations() {
            return ((Items[4] as IConditionable).Conditions[0] as LoopCondition);
        }

        public OpenCover GetOpenCoverItem() {
            return (Items[5] as OpenCover);
        }

        public override object Clone() {
            var clone = new TrainedDarkFlatExposure(
                this,
                profileService,
                cameraMediator,
                (CloseCover)this.GetCloseCoverItem().Clone(),
                (ToggleLight)this.GetToggleLightOffItem().Clone(),
                (SwitchFilter)this.GetSwitchFilterItem().Clone(),
                (SetBrightness)this.GetSetBrightnessItem().Clone(),
                (TakeExposure)this.GetExposureItem().Clone(),
                (LoopCondition)this.GetIterations().Clone(),
                (OpenCover)this.GetOpenCoverItem().Clone()
            ) {
                KeepPanelClosed = KeepPanelClosed,
            };
            return clone;
        }

        private string iterationsExpr = "1";

        [JsonProperty]
        public string IterationsExpr {
            get => iterationsExpr;
            set {
                iterationsExpr = value;
                ConstantExpression.Evaluate(this, "IterationsExpr", "IterationCount", 0);
                RaisePropertyChanged();
            }
        }
        [JsonProperty]
        public int IterationCount {
            get => GetIterations().Iterations;
            set {
                //
                if (Items.Count == 0) return;
                LoopCondition loop = GetIterations();
                if (loop != null) {
                    loop.Iterations = value;
                }
                RaisePropertyChanged("IterationCount");
            }
        }

        private string gainExpr = "";
        [JsonProperty]
        public string GainExpr {
            get => gainExpr;
            set {
                if (cameraMediator == null) return;
                gainExpr = value;
                ConstantExpression.Evaluate(this, "GainExpr", "Gain", cameraMediator.GetInfo().DefaultGain);
                RaisePropertyChanged("GainExpr");
            }
        }

        private int gain = -1;

        [JsonProperty]
        public int Gain {
            get => gain;
            set {
                gain = value;
                RaisePropertyChanged();
            }
        }


        public string ValidateGain(double gain) {
            return iValidateGain(gain, new List<string>());
        }

        public string iValidateGain(double gain, List<string> i) {
            var iCount = i.Count;

            CameraInfo = this.cameraMediator.GetInfo();
            if (!CameraInfo.Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            } else if (Gain < -1) {
                i.Add("Gain cannot be less than -1");
            } else if (CameraInfo.CanSetGain && Gain > -1 && (Gain < CameraInfo.GainMin || Gain > CameraInfo.GainMax)) {
                i.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_Gain"], CameraInfo.GainMin, CameraInfo.GainMax, Gain));
            }

            if (iCount == i.Count) {
                return String.Empty;
            } else {
                return i[iCount];
            }
        }

        private CameraInfo cameraInfo;
        public CameraInfo CameraInfo {
            get => cameraInfo;
            private set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }
        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var loop = GetIterations();
            if (loop.CompletedIterations >= loop.Iterations) {
                Logger.Warning($"The Trained Dark Flat Exposure progress is already complete ({loop.CompletedIterations}/{loop.Iterations}). The instruction will be skipped");
                throw new SequenceItemSkippedException($"The Trained Dark Flat Exposure progress is already complete ({loop.CompletedIterations}/{loop.Iterations}). The instruction will be skipped");
            }

            /* Lookup trained values and set brightness and exposure time accordingly */
            var filter = GetSwitchFilterItem()?.Filter;
            var takeExposure = GetExposureItem();
            var binning = takeExposure.Binning;
            var gain = takeExposure.Gain == -1 ? profileService.ActiveProfile.CameraSettings.Gain ?? -1 : takeExposure.Gain;
            var offset = takeExposure.Offset == -1 ? profileService.ActiveProfile.CameraSettings.Offset ?? -1 : takeExposure.Offset;
            var info = profileService.ActiveProfile.FlatDeviceSettings.GetTrainedFlatExposureSetting(filter?.Position, binning, gain, offset);

            (Items[3] as SetBrightness).Brightness = 0;
            takeExposure.ExposureTime = info.Time;
            takeExposure.ExposureTimeExpr = info.Time.ToString();

            if (KeepPanelClosed) {
                GetOpenCoverItem().Skip();
            } else {
                GetOpenCoverItem().ResetProgress();
            }

            /* Panel most likely cannot open/close so it should just be skipped */
            var closeItem = GetCloseCoverItem();
            if (!closeItem.Validate()) {
                closeItem.Skip();
            }
            var openItem = GetOpenCoverItem();
            if (!openItem.Validate()) {
                openItem.Skip();
            }

            var toggleLightOff = GetToggleLightOffItem();
            if (!toggleLightOff.Validate()) {
                toggleLightOff.Skip();
                GetSetBrightnessItem().Skip();
            }

            return base.Execute(progress, token);
        }

        public override bool Validate() {
            var switchFilter = GetSwitchFilterItem();
            var takeExposure = GetExposureItem();
            var setBrightness = GetSetBrightnessItem();

            var valid = takeExposure.Validate() && switchFilter.Validate() && setBrightness.Validate();

            var issues = new List<string>();

            if (valid) {
                var filter = switchFilter?.Filter;
                var binning = takeExposure.Binning;
                var gain = takeExposure.Gain == -1 ? profileService.ActiveProfile.CameraSettings.Gain ?? -1 : takeExposure.Gain;
                var offset = takeExposure.Offset == -1 ? profileService.ActiveProfile.CameraSettings.Offset ?? -1 : takeExposure.Offset;


                if (profileService.ActiveProfile.FlatDeviceSettings.GetTrainedFlatExposureSetting(filter?.Position, binning, gain, offset) == null) {
                    issues.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Validation_FlatDeviceTrainedExposureNotFound"], filter?.Name, gain, binning?.Name));
                    valid = false;
                }
            }

            Issues = issues.Concat(takeExposure.Issues).Concat(switchFilter.Issues).Concat(setBrightness.Issues).Distinct().ToList();
            RaisePropertyChanged(nameof(Issues));

            return valid;
        }

        /// <summary>
        /// When an inner instruction interrupts this set, it should reroute the interrupt to the real parent set
        /// </summary>
        /// <returns></returns>
        public override Task Interrupt() {
            return this.Parent?.Interrupt();
        }
    }
}