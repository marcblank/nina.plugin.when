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
using NINA.Sequencer.Validations;
using NINA.Core.Utility;
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
using NINA.Equipment.Interfaces;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Sequencer.SequenceItem;
using NINA.CustomControlLibrary;
using NCalc.Domain;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Set Switch Value +")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Switch_SetSwitchValue_Description")]
    [ExportMetadata("Icon", "ButtonSVG")]
    [ExportMetadata("Category", "Powerups (Enhanced Instructions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SetSwitchValue : SequenceItem, IValidatable {
        private ISwitchMediator switchMediator;

        [ImportingConstructor]
        public SetSwitchValue(ISwitchMediator switchMediator) {
            this.switchMediator = switchMediator;

            WritableSwitches = new ReadOnlyCollection<IWritableSwitch>(CreateDummyList());
            SelectedSwitch = WritableSwitches.First();
            ValueExpr = new Expr(this);
        }

        private SetSwitchValue(SetSwitchValue cloneMe) : this(cloneMe.switchMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            SetSwitchValue clone = new SetSwitchValue(this) { };
            clone.ValueExpr = new Expr(clone, this.ValueExpr.Expression);
            clone.ValueExpr.Setter = ValueSetter;
            clone.SwitchIndex = SwitchIndex;
            return clone;
        }

        public void ValueSetter(Expr expr) {
            if (Parent == null) {
                if (expr.SequenceEntity is SetSwitchValue ssv) {
                    AttachNewParent(expr.SequenceEntity.Parent);
                    SwitchIndex = ssv.SwitchIndex;
                }
            }
            expr.Error = null;
            if (expr.Value < SelectedSwitch.Minimum || expr.Value > SelectedSwitch.Maximum) {
                expr.Error = "Value must be between " + SelectedSwitch.Minimum + " and " + SelectedSwitch.Maximum;
            }
        }

        private Expr _ValueExpr = null;

        [JsonProperty]
        public Expr ValueExpr {
            get => _ValueExpr;
            set {
                _ValueExpr = value;
                value.Evaluate();
                RaisePropertyChanged();
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


        [JsonProperty]
        public double Value {
            get => 0;
            set {
                ValueExpr.Value = value;
                RaisePropertyChanged("ValueExpr");
            }
        }

        private short switchIndex;

        [JsonProperty]
        public short SwitchIndex {
            get => switchIndex;
            set {
                if (value > -1) {
                    switchIndex = value;
                    RaisePropertyChanged();
                }
            }
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Validate();
        }

        private IWritableSwitch selectedSwitch;

        [JsonIgnore]
        public IWritableSwitch SelectedSwitch {
            get => selectedSwitch;
            set {
                selectedSwitch = value;
                SwitchIndex = (short)(WritableSwitches?.IndexOf(selectedSwitch) ?? -1);
                RaisePropertyChanged();
            }
        }

        private ReadOnlyCollection<IWritableSwitch> writableSwitches;

        public ReadOnlyCollection<IWritableSwitch> WritableSwitches {
            get => writableSwitches;
            set {
                writableSwitches = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            ValueExpr.Evaluate();
            return switchMediator.SetSwitchValue(switchIndex, ValueExpr.Value, progress, token);
        }

        private IList<IWritableSwitch> CreateDummyList() {
            var dummySwitches = new List<IWritableSwitch>();
            for (short i = 0; i < 20; i++) {
                dummySwitches.Add(new DummySwitch((short)(i + 1)));
            }
            return dummySwitches;
        }

        public bool Validate() {
            try {
                var i = new List<string>();
                var info = switchMediator.GetInfo();
                if (info?.Connected != true) {
                    //When switch gets disconnected the real list will be changed to the dummy list
                    if (!(WritableSwitches.FirstOrDefault() is DummySwitch)) {
                        WritableSwitches = new ReadOnlyCollection<IWritableSwitch>(CreateDummyList());
                    }

                    i.Add(Loc.Instance["LblSwitchNotConnected"]);
                } else {
                    if (WritableSwitches.Count > 0) {
                        //When switch gets connected the dummy list will be changed to the real list
                        if (WritableSwitches.FirstOrDefault() is DummySwitch) {
                            WritableSwitches = info.WritableSwitches;

                            if (switchIndex >= 0 && WritableSwitches.Count > switchIndex) {
                                SelectedSwitch = WritableSwitches[switchIndex];
                            } else {
                                SelectedSwitch = null;
                            }
                        }
                    } else {
                        SelectedSwitch = null;
                        i.Add(Loc.Instance["Lbl_SequenceItem_Validation_NoWritableSwitch"]);
                    }
                }

                if (switchIndex >= 0 && WritableSwitches.Count > switchIndex) {
                    if (WritableSwitches[switchIndex] != SelectedSwitch) {
                        SelectedSwitch = WritableSwitches[switchIndex];
                    }
                }

                var s = SelectedSwitch;

                if (s == null) {
                    i.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Validation_NoSwitchSelected"]));
                } else {
                    if (ValueExpr.Value < s.Minimum || ValueExpr.Value > s.Maximum)
                        i.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Validation_InvalidSwitchValue"], s.Minimum, s.Maximum, s.StepSize));
                }

                ValueExpr.Validate();

                Issues = i;
                RaisePropertyChanged("Issues");
                return Issues.Count == 0;
            } catch (Exception ex) {
                Issues = new List<string>() { "An unexpected error occurred" };
                Logger.Error(ex);
                return false;
            }
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SetSwitchValue)}, SwitchIndex {SwitchIndex}, Value: {ValueExpr.Value}";
        }
    }
}