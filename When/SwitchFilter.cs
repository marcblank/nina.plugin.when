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
using NINA.Profile.Interfaces;
using NINA.Sequencer.Validations;
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
using NINA.Core.Model.Equipment;
using NINA.Core.Locale;
using System.Windows;
using NINA.Core.Utility;
using NINA.Sequencer.SequenceItem;
using NINA.Equipment.Equipment.MyFilterWheel;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Switch Filter +")]
    [ExportMetadata("Description", "Lbl_SequenceItem_FilterWheel_SwitchFilter_Description")]
    [ExportMetadata("Icon", "FW_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Enhanced Instructions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SwitchFilter : SequenceItem, IValidatable {

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            MatchFilter();
        }

        [ImportingConstructor]
        public SwitchFilter(IProfileService profileservice, IFilterWheelMediator filterWheelMediator) {
            ProfileService = profileservice;
            FilterWheelMediator = filterWheelMediator;

            WeakEventManager<IProfileService, EventArgs>.AddHandler(ProfileService, nameof(ProfileService.ProfileChanged), ProfileService_ProfileChanged);
        }

        private void MatchFilter() {
            try {
                var idx = FInfo?.Position ?? -1;
                FInfo = ProfileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters?.FirstOrDefault(x => x.Name == FInfo?.Name);
                if (FInfo == null && idx >= 0) {
                    FInfo = ProfileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters?.FirstOrDefault(x => x.Position == idx);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            MatchFilter();
        }

        private SwitchFilter(SwitchFilter cloneMe) : this(ProfileService, FilterWheelMediator) {
            CopyMetaData(cloneMe);
            FilterExpr = cloneMe.FilterExpr;
        }

        public override object Clone() {
            return new SwitchFilter(this) {
                Filter = Filter
            };
        }

        public static IProfileService ProfileService;
        public static IFilterWheelMediator FilterWheelMediator;

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        private List<string> iFilterNames = new List<string>();
        public List<string> FilterNames {
            get => iFilterNames;
            set {
                iFilterNames = value;
            }
        }

        public FilterInfo FInfo { get; set; } = new FilterInfo();

        public bool CVFilter { get; set; } = false;

        private string iFilterExpr;
        [JsonProperty]
        public string FilterExpr {
            get => iFilterExpr;
            set {
                if (value == null) return;
                // Have to massage this...
                // If begins with Filter_ then we look it up via ConstantExpression
                // Otherwise, we look it up in FilterWheelInfo
                iFilterExpr = value;

                // Find in FilterWheelInfo
                var fwi = ProfileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                Filter = -1;
                CVFilter = false;
                foreach (var fw in fwi) {
                    if (fw.Name.Equals(value)) {
                        Filter = fw.Position;
                        break;
                    }
                }
                if (Filter == -1) {
                    if (value.Equals("(Current)")) {
                        FilterWheelInfo filterWheelInfo = FilterWheelMediator.GetInfo();
                        if (filterWheelInfo.Connected) {
                            Filter = filterWheelInfo.SelectedFilter.Position;
                        }
                        FInfo = filterWheelInfo.SelectedFilter;
                    } else {
                        ConstantExpression.Evaluate(this, "FilterExpr", "Filter", -1);
                        if (Filter >= 0 && Filter < fwi.Count) {
                            FInfo = fwi[Filter];
                        }
                        CVFilter = true;
                    }
                } else {
                    FInfo = fwi[Filter];
                }

                RaisePropertyChanged(nameof(CVFilter));
                RaisePropertyChanged();
            }
        }

        private int iFilter = -1;
        public int Filter {
            get => iFilter;
            set {
                iFilter = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return FInfo == null
                ? throw new SequenceItemSkippedException("Skipping SwitchFilter - No Filter was selected")
                : FilterWheelMediator.ChangeFilter(FInfo, token, progress);
        }

        public bool Validate() {
            var i = new List<string>();
            if (FInfo != null && !FilterWheelMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblFilterWheelNotConnected"]);
            }

            if (FilterNames.Count == 0) {
                var fwi = ProfileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                foreach (var fw in fwi) {
                    FilterNames.Add(fw.Name);
                }
                RaisePropertyChanged("FilterNames");
            }

            if (CVFilter) {
                ConstantExpression.Evaluate(this, "FilterExpr", "Filter", -1, i);
            }

            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {            
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SwitchFilter)}, Filter: {FInfo?.Name}";
        }
    }
}