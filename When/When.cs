#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.Validations;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using NINA.Core.Enum;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Trigger;
using Newtonsoft.Json;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Equipment.Equipment.MyCamera;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "When")]
    [ExportMetadata("Description", "Runs a customizable set of instructions when the specified Expression is true.")]
    [ExportMetadata("Icon", "ShieldSVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]

    public class WhenSwitch : When, IValidatable, ICameraConsumer {

        [ImportingConstructor]
        public WhenSwitch(ISafetyMonitorMediator safetyMediator, ISequenceMediator sequenceMediator, IApplicationStatusMediator applicationStatusMediator, ISwitchMediator switchMediator,
                IWeatherDataMediator weatherMediator, ICameraMediator cameraMediator)
            : base(safetyMediator, sequenceMediator, applicationStatusMediator, switchMediator, weatherMediator, cameraMediator) {
            cameraMediator.RegisterConsumer(this);
        }

        public ICameraConsumer cameraConsumer {  get; set; } 

        protected WhenSwitch(WhenSwitch cloneMe) : base(cloneMe.safetyMediator, cloneMe.sequenceMediator, cloneMe.applicationStatusMediator, cloneMe.switchMediator, cloneMe.weatherMediator, cloneMe.cameraMediator) {
            if (cloneMe != null) {
                CopyMetaData(cloneMe);
                Instructions = (IfContainer)cloneMe.Instructions.Clone();
                Instructions.AttachNewParent(Parent);
                Instructions.PseudoParent = this;
                Instructions.Name = cloneMe.Name;
                Instructions.Icon = cloneMe.Icon;
                Predicate = cloneMe.Predicate;
                OnceOnly = cloneMe.OnceOnly;
            }
        }

        public bool Disabled { get; set; } = false;

        private bool iOnceOnly = false;

        [JsonProperty]
        public bool OnceOnly {
            get => iOnceOnly;
            set {
                iOnceOnly = value;
                RaisePropertyChanged();
            }
        }
        
        private string iPredicate = "";

        [JsonProperty]
        public string Predicate {
            get => iPredicate;
            set {
                iPredicate = value;
                RaisePropertyChanged("Predicate");
            }
        }

        private string iPredicateValue;

        public string PredicateValue {
            get { return iPredicateValue; }
            set {
                iPredicateValue = value;
                RaisePropertyChanged(nameof(PredicateValue));

            }
        }

        public string ValidateConstant(double temp) {
            if ((int)temp == 0) {
                return "False";
            } else if ((int)temp == 1) {
                return "True";
            }
            return string.Empty;
        }
        public override object Clone() {
            return new WhenSwitch(this);
        }

        protected bool IsActive() {
            return ItemUtility.IsInRootContainer(Parent) && Parent.Status == SequenceEntityStatus.RUNNING && Status != SequenceEntityStatus.DISABLED;
        }

        public override bool Check() {
            if (Disabled) return true;

            object result = ConstantExpression.Evaluate(this, "Predicate", "PredicateValue", 0);

            if (result == null) {
                return true;
            }
            if (!string.Equals(PredicateValue, "0", StringComparison.OrdinalIgnoreCase)) {
                Logger.Info("When: Check, PredicateValue = " + PredicateValue);
                if (OnceOnly) {
                    Disabled = true;
                }
                return false;
            }
            return true;
        }

        public string ShowCurrentInfo() {
            try {
                object result = ConstantExpression.Evaluate(this, "Predicate", "PredicateValue", 0);
                if (result is Boolean b && !b) {
                    return "There is a syntax error in the expression.";
                } else {
                    return "Your expression is currently: " + (PredicateValue.Equals("0") ? "False" : "True");
                }
            } catch (Exception ex) {
                return "Error: " + ex.Message;
            }
        }
        public IList<string> Switches { get; set; } = null;
        public new bool Validate() {

            CommonValidate();

            var i = new List<string>();

            if (string.IsNullOrEmpty(Predicate)) {
                i.Add("Expression cannot be empty!");
            }

            try {
                ConstantExpression.Evaluate(this, "Predicate", "PredicateValue", 0);
            } catch (Exception ex) {
                i.Add("Error in expression: " + ex.Message);
            }

            Switches = ConstantExpression.GetSwitches();
            RaisePropertyChanged("Switches");

            Issues = i;
            return i.Count == 0;
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }
    }
}
