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
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Guider;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Utility;
using NINA.Core.Utility;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.Trigger;

namespace PowerupsLite.When {

    [ExportMetadata("Name", "Dither After Exposures +")]
    [ExportMetadata("Description", "Lbl_SequenceTrigger_Guider_DitherAfterExposures_Description")]
    [ExportMetadata("Icon", "DitherSVG")]
    [ExportMetadata("Category", "Powerups (Enhanced Instructions)")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class DitherAfterExposures : SequenceTrigger, IValidatable {
        private IGuiderMediator guiderMediator;
        private IImageHistoryVM history;
        private IProfileService profileService;

        [ImportingConstructor]
        public DitherAfterExposures(IGuiderMediator guiderMediator, IImageHistoryVM history, IProfileService profileService) : base() {
            this.guiderMediator = guiderMediator;
            this.history = history;
            this.profileService = profileService;
            AfterExposures = 1;
            TriggerRunner.Add(new Dither(guiderMediator, profileService));
            AfterExpr = new Expr(this);
        }

        private DitherAfterExposures(DitherAfterExposures cloneMe) : this(cloneMe.guiderMediator, cloneMe.history, cloneMe.profileService) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            var clone = new DitherAfterExposures(this);
            clone.TriggerRunner = (SequentialContainer)TriggerRunner.Clone();
            clone.AfterExpr = new Expr(clone, this.AfterExpr.Expression, "Integer", SetAfter, 1);
            return clone;
        }

        public void SetAfter(Expr expr) {
            RaisePropertyChanged("AfterExposures");
        }


        private Expr _AfterExpr = null;

        [JsonProperty]
        public Expr AfterExpr {
            get => _AfterExpr;
            set {
                _AfterExpr = value;
                RaisePropertyChanged();
            }
        }

        public int AfterExposures {
            get => (int)AfterExpr.Value;
            set {
                if (AfterExpr != null) {
                    AfterExpr.Expression = value.ToString();
                }
            }
        }

        private int lastTriggerId = 0;


        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = ImmutableList.CreateRange(value);
                RaisePropertyChanged();
            }
        }

        public int ProgressExposures => AfterExposures > 0 ? history.ImageHistory.Count % AfterExposures : 0;

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            AfterExpr.Validate();
            RaisePropertyChanged("AfterExposures");
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (AfterExpr.Value > 0) {
                lastTriggerId = history.ImageHistory.Count;
                await TriggerRunner.Run(progress, token);
            } else {
                return;
            }
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (nextItem == null) { return false; }
            if (!(nextItem is IExposureItem exposureItem)) { return false; }
            if (exposureItem.ImageType != "LIGHT") { return false; }

            RaisePropertyChanged(nameof(ProgressExposures));
            if (lastTriggerId > history.ImageHistory.Count) {
                // The image history was most likely cleared
                lastTriggerId = 0;
            }
            var shouldTrigger = lastTriggerId < history.ImageHistory.Count && history.ImageHistory.Count > 0 && ProgressExposures == 0;

            if (shouldTrigger) {
                if (ItemUtility.IsTooCloseToMeridianFlip(Parent, TriggerRunner.GetItemsSnapshot().First().GetEstimatedDuration() + nextItem?.GetEstimatedDuration() ?? TimeSpan.Zero)) {
                    Logger.Warning("Dither should be triggered, however the meridian flip is too close to be executed");
                    shouldTrigger = false;
                }
            }

            return shouldTrigger;
        }

        public override string ToString() {
            return $"Trigger: {nameof(DitherAfterExposures)}, After Exposures: {AfterExposures}";
        }

        public bool Validate() {
            var i = new List<string>();
            var info = guiderMediator.GetInfo();

            if (AfterExposures > 0 && !info.Connected) {
                i.Add(Loc.Instance["LblGuiderNotConnected"]);
            }

            AfterExpr.Validate();
            RaisePropertyChanged("AfterExposures");

            Issues = i;
            return i.Count == 0;
        }
    }
}