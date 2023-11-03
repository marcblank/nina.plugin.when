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

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Loop While")]
    [ExportMetadata("Description", "Loops while the specified expression is not false.")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Constants Enhanced")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]

    public class LoopWhile : SequenceCondition {

        [ImportingConstructor]
        public LoopWhile() {
            Predicate = "";
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

            //var i = new List<string>();

            //if (Predicate.IsNullOrEmpty()) {
            //    i.Add("Expression cannot be empty!");
            //}

            //try {
            //    ConstantExpression.Evaluate(this, "Predicate", "PredicateValue", 0);
            //} catch (Exception ex) {
            //    i.Add("Error in expression: " + ex.Message);
            //}

            //Issues = i;
            //return i.Count == 0;
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

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {

            if (Predicate.IsNullOrEmpty()) {
                Status = SequenceEntityStatus.FAILED;
                Logger.Info("LoopWhile: Check, Predicate is null or empty");
                return false;
            }

            try {
                object result = ConstantExpression.Evaluate(this, "Predicate", "PredicateValue", 0);
                Logger.Info("LoopWhile: Check, PredicateValue = " + PredicateValue);
                if (result == null) {
                    // Syntax error...
                    Logger.Info("LoopWhile: There is a syntax error in your predicate expression.");
                    Status = NINA.Core.Enum.SequenceEntityStatus.FAILED;
                    return false;
                }

                if (!string.Equals(PredicateValue, "0", StringComparison.OrdinalIgnoreCase)) {
                    Logger.Info("LoopWhile: Predicate is true!");
                    return true;
                } else {
                    Logger.Info("LoopWhile: Predicate is false!");
                    return false;
                }
            } catch (ArgumentException ex) {
                Logger.Info("LoopWhile error: " + ex.Message);
                Status = SequenceEntityStatus.FAILED;
                return false;
            }
        }

    }
}
