using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using NCalc;
using Castle.Core.Internal;
using NINA.Core.Utility.Notification;
using NINA.Core.Enum;
using System.Linq;
using System.Text;
using Accord.IO;
using Namotion.Reflection;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Interfaces;
using NINA.Sequencer.SequenceItem.Utility;
using System.Windows;
using NINA.Equipment.Equipment.MyWeatherData;
using System.Windows.Controls;
using System.Diagnostics;
using NINA.Core.Utility;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Utility;
using System.Diagnostics.Eventing.Reader;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Loop While")]
    [ExportMetadata("Description", "Loops while the specified expression is not false.")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Constants Enhanced")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]

    public class LoopWhile : SequenceCondition, IValidatable {

        [ImportingConstructor]
        public LoopWhile() {
            Predicate = "";
            ConditionWatchdog = new ConditionWatchdog(InterruptWhenFails, TimeSpan.FromSeconds(5));
        }

        public LoopWhile(LoopWhile copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Predicate = copyMe.Predicate;
            }
        }

        public override object Clone() {
            return new LoopWhile(this) {
            };
        }

        public string ValidateConstant(double temp) {
            if ((int)temp == 0) {
                return "False";
            } else if ((int)temp == 1) {
                return "True";
            }
            return string.Empty;
        }

        [JsonProperty]
        public string Predicate { get; set; }

        [JsonProperty]
        private string iPredicateValue;

        public string PredicateValue {
            get { return iPredicateValue; }
            set {
                iPredicateValue = value;
                RaisePropertyChanged(nameof(PredicateValue));

            }
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(LoopWhile)}, Predicate: {Predicate}";
        }


        public IList<string> Issues { get; set; }

        public bool Validate() {

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

        private bool Debugging = false;

        private void LogInfo(string str) {
            if (Debugging) {
                Logger.Info(str);
            }
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {

            if (Predicate.IsNullOrEmpty()) {
                Status = SequenceEntityStatus.FAILED;
                LogInfo("LoopWhile: Check, Predicate is null or empty");
                return false;
            }

            try {
                object result = ConstantExpression.Evaluate(this, "Predicate", "PredicateValue", 0);
                //Logger.Info("LoopWhile: Check, PredicateValue = " + PredicateValue);
                if (result == null) {
                    // Syntax error...
                    LogInfo("LoopWhile: There is a syntax error in your predicate expression.");
                    Status = NINA.Core.Enum.SequenceEntityStatus.FAILED;
                    return false;
                }

                if (!string.Equals(PredicateValue, "0", StringComparison.OrdinalIgnoreCase)) {
                    LogInfo("LoopWhile: Predicate is true!");
                    return true;
                } else {
                    LogInfo("LoopWhile: Predicate is false!");
                    return false;
                }
            } catch (ArgumentException ex) {
                LogInfo("LoopWhile error: " + ex.Message);
                Status = SequenceEntityStatus.FAILED;
                return false;
            }
        }

        public override void AfterParentChanged() {
            if (Parent == null) {
                SequenceBlockTeardown();
            } else {
                if (Parent.Status == SequenceEntityStatus.RUNNING) {
                    SequenceBlockInitialize();
                }
            }
        }

        public override void SequenceBlockTeardown() {
            try { ConditionWatchdog?.Cancel(); } catch { }
        }

        public override void SequenceBlockInitialize() {
            ConditionWatchdog?.Start();
        }

        public IList<string> Switches { get; set; } = null;

        private async Task InterruptWhenFails() {
 
            if (!Check(null, null)) {
                if (this.Parent != null) {
                    if (ItemUtility.IsInRootContainer(Parent) && this.Parent.Status == SequenceEntityStatus.RUNNING && this.Status != SequenceEntityStatus.DISABLED) {
                        Logger.Info("Expression returned false - Interrupting current Instruction Set");
                        await this.Parent.Interrupt();
                    }
                }
            }
        }

    }
}
