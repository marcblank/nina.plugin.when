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
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SetConstant : SequenceItem, IValidatable {
        [ImportingConstructor]
        public SetConstant() {
            Constant = "";
        }
        public SetConstant(SetConstant copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                CValueExpr = copyMe.CValueExpr;
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
                    if (ConstantExpression.IsValid(this, Dummy, value, out double val, null)) {
                    }
                    constant = value;
                    ConstantExpression.UpdateConstants(this);
                }
                RaisePropertyChanged();
            }
        }
 
        private string cValueExpr = "0";
        [JsonProperty]
        public string CValueExpr {
            get => cValueExpr;
            set {
                cValueExpr = value;
                ConstantExpression.UpdateConstants(this);
                ConstantExpression.Evaluate(this, "CValueExpr", "CValue");
                RaisePropertyChanged("CValueExpr");
            }
        }

        private string cValue = "";

        [JsonProperty]
        public string CValue {
            get => cValue;
            set {
                cValue = value;
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

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            // Nothing to do here
            return Task.CompletedTask;
        }

        public override object Clone() {
            return new SetConstant(this) {
                Constant = Constant,
                CValueExpr = CValueExpr
            };
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SetConstant)}, Constant: {Constant}, Value: {CValueExpr}";
        }

        public bool Validate() {
            var i = new List<string>();
            if (ConstantExpression.Evaluate(this, "CValueExpr", "CValue")) {
                CValue = CValueExpr;
            }
            Issues = i;
            return i.Count == 0;
        }
    }
}
