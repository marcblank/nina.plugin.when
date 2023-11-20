using WhenPlugin.When;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Sequencer.Container;
using NINA.Core.Enum;
using System.Windows.Forms;
using NINA.Core.Utility;
using System.Text;
using Accord;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Define Variable")]
    [ExportMetadata("Description", "Defines a variable whose numeric value can be used in various instructions")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SetVariable : SequenceItem, IValidatable, ISettable {
        [ImportingConstructor]
        public SetVariable() {
            Variable = "";
            Icon = Icon;
        }
        public SetVariable(SetVariable copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                CValueExpr = copyMe.CValueExpr;
                OValueExpr = copyMe.OValueExpr;
                Icon = copyMe.Icon;
            }
        }

        public string Dummy;

        public static WhenPlugin WhenPluginObject { get; set; }

        private string variable;

        public bool IsSetvariable { get; set; } = false;

        [JsonProperty]
        public string Variable {
            get => variable;
            set {
                if (value == variable) {
                    return;
                }
                variable = value;
                RaisePropertyChanged();
            }
        }

        public bool DuplicateName { get; set; } = false;

        private string cValueExpr = "0";
        [JsonProperty]
        public string CValueExpr {
            get => cValueExpr;
            set {
                if (cValueExpr == value) {
                    RaisePropertyChanged("CValue");
                    RaisePropertyChanged("CValueExpr");
                    return;
                }
                cValueExpr = value;
                RaisePropertyChanged("OValueExpr");
                RaisePropertyChanged("CValueExpr");
                ConstantExpression.GlobalContainer.Validate();
            }
        }
 
        private string oValueExpr = "0";
        [JsonProperty]
        public string OValueExpr {
            get => oValueExpr;
            set {
                if (oValueExpr == value) {
                    return;
                }
                oValueExpr = value;
                //CValueExpr = value;
                RaisePropertyChanged("OValueExpr");
                //RaisePropertyChanged("CValueExpr");
                ConstantExpression.GlobalContainer.Validate();
            }
        }


        public string ValidateVariable(double var) {
            if (Status != SequenceEntityStatus.FINISHED) {
                return "Not Yet Defined";
            }
            return String.Empty;
        }

        private string cValue = "Undefined";
        [JsonProperty]
        public string CValue {
            get => cValue;
            set {
                cValue = value;
                RaisePropertyChanged();
            }
        }

          
        private string oValue = "Undefined";
        [JsonProperty]
        public string OValue {
            get => oValue;
            set {
                oValue = value;
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
            // Signal that the variable is valid
            CValueExpr = OValueExpr;
            ConstantExpression.Evaluate(this, "CValueExpr", "CValue", "");
            ConstantExpression.Evaluate(this, "OValueExpr", "OValue", "");
            Status = SequenceEntityStatus.FINISHED;
            ConstantExpression.FlushKeys();
            ConstantExpression.UpdateConstants(this);
            ConstantExpression.GlobalContainer.Validate();
            RaisePropertyChanged("CValueExpr");
            RaisePropertyChanged("CValue");
            return Task.CompletedTask;
        }

        public override object Clone() {
            return new SetVariable(this) {
                Variable = variable,
                CValueExpr = CValueExpr,
                OValueExpr = OValueExpr
            };
        }

        public override void ResetProgress() {
            base.ResetProgress();
            CValueExpr = "";
            CValue = "";
            RaisePropertyChanged("CValueExpr");
            RaisePropertyChanged("CValue");
            ConstantExpression.FlushKeys();
            ConstantExpression.UpdateConstants(this);
            ConstantExpression.GlobalContainer.Validate();
        }

        private bool IsAttachedToRoot() {
            ISequenceContainer p = Parent;
            while (p != null) {
                if (p is SequenceRootContainer) {
                    return true;
                }
                p = p.Parent;
            }
            return false;
        }

        private ISequenceContainer LastParent { get; set; }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            LastParent = Parent;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SetVariable)}, Variable: {variable}, ValueExpr: {CValueExpr}, Value: {CValue}";
        }

        public bool Validate() {
            if (!IsAttachedToRoot()) return true;

            var i = new List<string>();

            if (DuplicateName) {
                i.Add("Duplicate name in the same instruction set!");
            }

            ConstantExpression.Keys k = ConstantExpression.GetSwitchWeatherKeys();
            if (k.ContainsKey(Variable)) {
                i.Add("The name '" + Variable + "' is reserved.");
                Logger.Info("Attempt to define reserved name: " + Variable);
                StringBuilder sb = new StringBuilder();
                foreach(var kk in k) {
                    sb.Append(kk.Key);
                    sb.Append(" ");
                }
                Logger.Info("Keys: " + sb.ToString());
            }

            Issues = i;
            if (Issues.Count > 0) {
                cValue = Double.NaN.ToString();
            }

            RaisePropertyChanged("CValueExpr");
            RaisePropertyChanged("CValue");
            RaisePropertyChanged("OValueExpr");
            RaisePropertyChanged("OValue");

            if (Issues.Count > 0) {
               var x = 0;
            }
            
            return Issues.Count == 0;
        }

        public string GetSettable() {
            return Variable;
        }

        public string GetValueExpression() {
            return CValueExpr;
        }

        public void IsDuplicate(bool val) {
            DuplicateName = val;
        }

        string ISettable.GetType() {
            return "Variable";
        }
    }
}
