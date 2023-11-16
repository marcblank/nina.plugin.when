#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using GalaSoft.MvvmLight.Command;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Core.Enum;
using NINA.Sequencer.Utility;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Container;
using NINA.Core.Model;
using NINA.Sequencer.Conditions;
using Newtonsoft.Json;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Equipment.MySafetyMonitor;
using System.ComponentModel;
using System.Reflection;
using Namotion.Reflection;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.ViewModel.Sequencer;
using System.Windows.Input;
using System.Management;
using System.Diagnostics;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Sequencer;
using System.Windows.Media.Converters;
using NINA.WPF.Base.Mediator;
using NINA.View.Sequencer.Converter;
using Castle.Core.Internal;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "When")]
    [ExportMetadata("Description", "Runs a customizable set of instructions when the specified Expression is true.")]
    [ExportMetadata("Icon", "ShieldSVG")]
    [ExportMetadata("Category", "Sequencer")]
    [Export(typeof(ISequenceTrigger))]

    public class WhenSwitch : When, IValidatable {

        [ImportingConstructor]
        public WhenSwitch(ISafetyMonitorMediator safetyMediator, ISequenceMediator sequenceMediator, IApplicationStatusMediator applicationStatusMediator, ISwitchMediator switchMediator,
                IWeatherDataMediator weatherMediator)
            : base(safetyMediator, sequenceMediator, applicationStatusMediator, switchMediator, weatherMediator) {
        }

        protected WhenSwitch(WhenSwitch cloneMe) : base(cloneMe.safetyMediator, cloneMe.sequenceMediator, cloneMe.applicationStatusMediator, cloneMe.switchMediator, cloneMe.weatherMediator) {
            if (cloneMe != null) {
                CopyMetaData(cloneMe);
                Instructions = (IfContainer)cloneMe.Instructions.Clone();
                Instructions.AttachNewParent(Parent);
                Instructions.PseudoParent = this;
                Instructions.Name = Name;
                Instructions.Icon = Icon;
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
        [JsonProperty]
        private string iPredicateValue;

        public string PredicateValue {
            get { return iPredicateValue; }
            set {
                iPredicateValue = value;
                RaisePropertyChanged(nameof(PredicateValue));

            }
        }

        public override object Clone() {
            return new WhenSwitch(this);
        }

        protected bool IsActive() {
            return ItemUtility.IsInRootContainer(Parent) && Parent.Status == SequenceEntityStatus.RUNNING && Status != SequenceEntityStatus.DISABLED;
        }


        public override bool Check() {
            if (Disabled) return false;

            object result = ConstantExpression.Evaluate(this, "Predicate", "PredicateValue", 0);

            Logger.Info("When: Check, PredicateValue = " + PredicateValue);
            if (result == null) {
                return false;
            }
            if (!string.Equals(PredicateValue, "0", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
            return false;
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

            if (Predicate.IsNullOrEmpty()) {
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
    }
}
