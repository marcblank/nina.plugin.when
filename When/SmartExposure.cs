#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Guider;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Sequencer.Utility;
using NINA.Sequencer.SequenceItem;
using System.Windows.Media;
using NINA.Core.Utility.ColorSchema;
using System.Windows;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Lbl_SequenceItem_Imaging_SmartExposure_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Imaging_SmartExposure_Description")]
    [ExportMetadata("Icon", "CameraSVG")]
    [ExportMetadata("Category", "Constants Enhanced")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SmartExposure : SequentialContainer, IImmutableContainer {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.Items.Clear();
            this.Conditions.Clear();
            this.Triggers.Clear();
        }

        [ImportingConstructor]
        public SmartExposure(
                IProfileService profileService,
                ICameraMediator cameraMediator,
                IImagingMediator imagingMediator,
                IImageSaveMediator imageSaveMediator,
                IImageHistoryVM imageHistoryVM,
                IFilterWheelMediator filterWheelMediator,
                IGuiderMediator guiderMediator) : this(
                    null,
                    new SwitchFilter(profileService, filterWheelMediator),
                    new TakeExposure(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM),
                    new LoopCondition(),
                    new DitherAfterExposures(guiderMediator, imageHistoryVM, profileService)
                ) {
        }

        /// <summary>
        /// Clone Constructor
        /// </summary>
        public SmartExposure(
                SmartExposure cloneMe,
                SwitchFilter switchFilter,
                TakeExposure takeExposure,
                LoopCondition loopCondition,
                DitherAfterExposures ditherAfterExposures
                ) {
            this.Add(switchFilter);
            this.Add(takeExposure);
            this.Add(loopCondition);
            this.Add(ditherAfterExposures);

            IsExpanded = false;

            if (cloneMe != null) {
                CopyMetaData(cloneMe);
                IterationsExpr = cloneMe.IterationsExpr;
                DitherExpr = cloneMe.DitherExpr;
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

        public SwitchFilter GetSwitchFilter() {
            return Items[0] as SwitchFilter;
        }

        public TakeExposure GetTakeExposure() {
            return Items[1] as TakeExposure;
        }

        public DitherAfterExposures GetDitherAfterExposures() {
            return Triggers[0] as DitherAfterExposures;
        }

        public LoopCondition GetLoopCondition() {
            return Conditions[0] as LoopCondition;
        }

        private string iterationsExpr = "1";

        [JsonProperty]
        public string IterationsExpr {
            get => iterationsExpr;
            set {
                iterationsExpr = value;
                ConstantExpression.Evaluate(this, "IterationsExpr", "IterationCount", 1);
                RaisePropertyChanged();
            }
        }

        private int iterationCount = 0;
        [JsonProperty]
        public int IterationCount {
            get => iterationCount;
            set {
                //
                if (Conditions.Count == 0) return;
                LoopCondition lc = Conditions[0] as LoopCondition;
                iterationCount = lc.Iterations = value;
                RaisePropertyChanged("IterationCount");
            }
        }

        private string ditherExpr = "1";

        [JsonProperty]
        public string DitherExpr {
            get => ditherExpr;
            set {
                ditherExpr = value;
                ConstantExpression.Evaluate(this, "DitherExpr", "DitherCount", 0);
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public int DitherCount {
            get => (Triggers[0] as DitherAfterExposures).AfterExposures;
            set {
                if (Triggers.Count == 0) return;
                DitherAfterExposures lc = Triggers[0] as DitherAfterExposures;
                lc.AfterExposures = value;
                RaisePropertyChanged("DitherCount");
            }
        }

        public override bool Validate() {
            var i = new List<string>();
            var sw = GetSwitchFilter();
            var te = GetTakeExposure();
            var dither = GetDitherAfterExposures();

            bool valid = true;

            valid = te.Validate() && valid;
            i.AddRange(te.Issues);

            if (sw.Filter != null) {
                valid = sw.Validate() && valid;
                i.AddRange(sw.Issues);
            }

            if (dither.AfterExposures > 0) {
                valid = dither.Validate() && valid;
                i.AddRange(dither.Issues);
            }
  
            ConstantExpression.Evaluate(this, "IterationsExpr", "IterationCount", 1, i);
            ConstantExpression.Evaluate(this, "DitherExpr", "DitherCount", 0, i);

            Issues = i;
            RaisePropertyChanged("Issues");
            return (Issues.Count == 0) && valid;
        }

        public override object Clone() {
            var clone = new SmartExposure(
                    this,
                    (SwitchFilter)this.GetSwitchFilter().Clone(),
                    (TakeExposure)this.GetTakeExposure().Clone(),
                    (LoopCondition)this.GetLoopCondition().Clone(),
                    (DitherAfterExposures)this.GetDitherAfterExposures().Clone()
                );
            return clone;
        }

        public override TimeSpan GetEstimatedDuration() {
            return GetTakeExposure().GetEstimatedDuration();
        }

        /// When an inner instruction interrupts this set, it should reroute the interrupt to the real parent set

        public override Task Interrupt() {
            return this.Parent?.Interrupt();
        }
    }
}