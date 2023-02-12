using WhenPlugin.When;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Define Constant")]
    [ExportMetadata("Description", "Sets a constant whose numeric value can be used in various instructions")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Sequencer")]
    //[Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SetConstant : SequenceItem, IValidatable {
        [ImportingConstructor]
        public SetConstant() {
            Constant = "";
        }
        public SetConstant(SetConstant copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                ValueExpr = copyMe.ValueExpr;
            }
        }

        [JsonProperty]
        public string Dummy;

        private string constant;

        [JsonProperty]
        public string Constant {
            get => constant;
            set {
                // ** Fix when Constant can be an expression
                if (constant != value) {
                    // Fixup values!
                    foreach (Consumer consumer in consumers) {
                        var prop = consumer.Item.GetType().GetProperty(consumer.Name);
                        prop.SetValue(consumer.Item, value);
                        RaisePropertyChanged(consumer.Name);
                    }
                    if (ConstantExpression.IsValidExpression(this, Dummy, value, out double val, null)) {
                        // Already defined!
                        //value = "'" + value + "' is defined elsewhere!";
                    }
                    constant = value;
                }
                RaisePropertyChanged();
            }
        }

        private bool isLoop() {
            if (valueExpr == null) return false;
            string[] tokens = Regex.Split(valueExpr, @"(?=[-+*/])|(?<=[-+*/])");
            foreach (string token in tokens) {
                if (token.Equals(Constant)) {
                    return true;
                }
            }
            return false;
        }

        private string valueExpr;

        [JsonProperty]
        public string ValueExpr {
            get => valueExpr;
            set {
                double val;
                valueExpr = value;
                if (value == null || isLoop()) {
                    Value = 0;
                } else if (ConstantExpression.IsValidExpression(this, nameof(ValueExpr), value, out val, null)) {
                    Value = val;
                }
                ConstantExpression.UpdateConstants(this);
                RaisePropertyChanged();
            }
        }

        public string ValueString {
            get {
                return Value.ToString();
            }
            set { }
        }

        private double iValue;

        [JsonProperty]
        public double Value {
            get => iValue;
            set {
                iValue = value;
                RaisePropertyChanged();
                RaisePropertyChanged("ValueString");
                foreach (Consumer consumers in consumers) {
                    object item = consumers.Item;
                    if (item is IValidatable) {
                        _ = (item as IValidatable).Validate();
                    }
                }
            }
        }

        // The Consumer class contains references to a SequenceItem (containing a Constant) and the name of the
        // property that refers to that Constant.  It's used in order to update consumers of the Constant in case
        // the value (or name, for that matter) of the Constant changes.
        public class Consumer {
            private SequenceItem item;
            private string name;

            public SequenceItem Item { get; set; }
            public string Name { get; set; }

            public Consumer(SequenceItem item, string name) {
                Item = item;
                Name = name;
            }
        }

        private IList<Consumer> consumers = new List<Consumer>();

        public void AddConsumer(SequenceItem item, string exprName) {
            foreach (Consumer consumer in consumers) {
                if (consumer.Item == item) {
                    return;
                }
            }
            consumers.Add(new Consumer(item, exprName));
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            // Nothing to do here
            return Task.CompletedTask;
        }

        public override object Clone() {
            return new SetConstant(this) {
                Constant = Constant,
                Value = Value
            };
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SetConstant)}, Constant: {Constant}, Value: {Value}";
        }

        private Brush isValidValue = Brushes.GreenYellow;
        [JsonProperty]
        public Brush IsValidValue { get => isValidValue; set => isValidValue = value; }


        public bool Validate() {
            var i = new List<string>();

            if (isLoop()) {
                Value = -1;
                isValidValue = Brushes.Orange;
                i.Add("I see what you're doing there...");
            } else if (ConstantExpression.IsValidExpression(this, nameof(ValueExpr), ValueExpr, out double expTime, i)) {
                Value = expTime;
                IsValidValue = Brushes.GreenYellow;
            } else {
                IsValidValue = Brushes.Orange;
                Value = -1;
            }
            RaisePropertyChanged();
            RaisePropertyChanged("IsValidValue");
            RaisePropertyChanged("Value");

            if (!Regex.IsMatch(Constant, "^[a-zA-Z0-9]*$")) {
                i.Add("Constant name must be alphanumeric");
            }

            Issues = i;
            return i.Count == 0;
        }
    }
}
