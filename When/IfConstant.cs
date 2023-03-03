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

namespace WhenPlugin.When {
    [ExportMetadata("Name", "If Constant")]
    [ExportMetadata("Description", "Executes an instruction set if the constant is True (or 1)")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Constants Enhanced")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class IfConstant : IfCommand, IValidatable {

        [ImportingConstructor]
        public IfConstant() {
            Predicate = "";
            Instructions = new IfContainer();
            Instructions.PseudoParent = this;
        }

        public IfConstant(IfConstant copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Predicate = copyMe.Predicate;
                Instructions = (IfContainer)copyMe.Instructions.Clone();
                Instructions.PseudoParent = this;
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

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            Logger.Info("IfConstant: Execute, Predicate = " + Predicate);
            if (Predicate.IsNullOrEmpty()) {
                Status = SequenceEntityStatus.FAILED;
                return;
            }

            try {
                object result = ConstantExpression.Evaluate(this, "Predicate", "PredicateValue", 0);
                Logger.Info("IfConstant: Execute, PredicateValue = " + PredicateValue);
                if (result == null) {
                    // Syntax error...
                    Logger.Info("IfConstant: There is a syntax error in your predicate expression.");
                    Status = NINA.Core.Enum.SequenceEntityStatus.FAILED;
                    return;
                }

                if (!string.Equals(PredicateValue, "0", StringComparison.OrdinalIgnoreCase)) {
                    Logger.Info("IfConstant: If Predicate is true!");
                    Runner runner = new Runner(Instructions, null, progress, token);
                    await runner.RunConditional();
                } else {
                    return;
                }
            } catch (ArgumentException ex) {
                Logger.Info("IfConstant error: " + ex.Message);
                Status = SequenceEntityStatus.FAILED;
            }
        }

        [JsonProperty]
        public string Predicate { get; set; }

        [JsonProperty]
        public string PredicateValue { get; set; }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(IfSwitch)}, Predicate: {Predicate}";
        }

        public new bool Validate() {
            var i = new List<string>();

            if (Instructions.PseudoParent == null) {
                Instructions.PseudoParent = this;
            }

            if (Predicate.IsNullOrEmpty()) {
                i.Add("Expression cannot be empty!");
            }

            try {
                ConstantExpression.Evaluate(this, "PredicateExpr", "PredicateValue", 0);
            } catch (Exception ex) {
                i.Add("Error in expression: " + ex.Message);
            }

            foreach (ISequenceItem item in Instructions.Items) {
                if (item is IValidatable val) {
                    _ = val.Validate();
                }
            }

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
