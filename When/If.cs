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
using System.Collections.ObjectModel;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "If")]
    [ExportMetadata("Description", "Executes an instruction set if the Expression is True (or 1)")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class IfConstant : IfCommand, IValidatable, IIfWhenSwitch {

        [ImportingConstructor]
        public IfConstant() {
            Predicate = "";
            Instructions = new IfContainer();
            Instructions.AttachNewParent(Parent);
            Instructions.PseudoParent = this;
            Instructions.Name = Name;
            Instructions.Icon = Icon;
        }

        public IfConstant(IfConstant copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Predicate = copyMe.Predicate;
                Instructions = (IfContainer)copyMe.Instructions.Clone();
                Instructions.AttachNewParent(Parent);
                Instructions.PseudoParent = this;
                Instructions.Name = copyMe.Name;
                Instructions.Icon = copyMe.Icon;
            }
        }

        public override object Clone() {
            return new IfConstant(this) {
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

        public bool Check() {

            object result = ConstantExpression.Evaluate(this, "Predicate", "PredicateValue", 0);

            Logger.Info("IfConstant: Check, PredicateValue = " + PredicateValue);
            if (result == null) {
               return false;
            }
            if (!string.Equals(PredicateValue, "0", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
            return false;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            Logger.Info("If: Execute, Predicate = " + Predicate);
            if (Predicate.IsNullOrEmpty()) {
                Status = SequenceEntityStatus.FAILED;
                return;
            }

            try {
                //// See if Predicate contains image parameters?
                //NCalc.Expression e = new NCalc.Expression(Predicate);
                //ConstantExpression.Keys k = new ConstantExpression.Keys();
                //ConstantExpression.GetParsedKeys(e.ParsedExpression, new ConstantExpression.Keys(), k);
                //if (k.ContainsKey("FWHM") || k.ContainsKey("HFR") || k.ContainsKey("Eccentricity") || k.ContainsKey("StarCount")) {
                //    // We need to wait...
                //    Logger.Info("Waiting for image data...");
                //}

                object result = ConstantExpression.Evaluate(this, "Predicate", "PredicateValue", 0);
                Logger.Info("IfConstant: Execute, PredicateValue = " + PredicateValue);
                if (result == null) {
                    // Syntax error...
                    Logger.Info("If: There is a syntax error in your expression.");
                    Status = NINA.Core.Enum.SequenceEntityStatus.FAILED;
                    return;
                }

                if (!string.Equals(PredicateValue, "0", StringComparison.OrdinalIgnoreCase)) {
                    Logger.Info("If: If Predicate is true!");
                    Runner runner = new Runner(Instructions, null, progress, token);
                    await runner.RunConditional();
                } else {
                    return;
                }
            } catch (ArgumentException ex) {
                Logger.Info("If error: " + ex.Message);
                Status = SequenceEntityStatus.FAILED;
            }
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
            return $"Category: {Category}, Item: {nameof(IfConstant)}, Predicate: {Predicate}";
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

    }
}
