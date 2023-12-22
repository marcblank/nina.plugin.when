using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Enum;
using NINA.Core.Utility;

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
            IfExpr = new Expr(this);
            Instructions = new IfContainer();
            Instructions.AttachNewParent(Parent);
            Instructions.PseudoParent = this;
            Instructions.Name = Name;
            Instructions.Icon = Icon;
        }

        public IfConstant(IfConstant copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                IfExpr = new Expr(this);
                IfExpr.Expression = copyMe.IfExpr.Expression;
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

             return false;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            //Logger.Info("If: Execute, Predicate = " + Predicate);
            //if (string.IsNullOrEmpty(Predicate)) {
            //    Status = SequenceEntityStatus.FAILED;
            //    return;
            //}

            try {
  
                if (!string.Equals(IfExpr.ValueString, "0", StringComparison.OrdinalIgnoreCase)) {
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

        private string iPredicate;
        [JsonProperty]
        public string Predicate {
            get => iPredicate;
            set {
                iPredicate = value;
                if (IfExpr == null) {
                    IfExpr = new Expr(this);
                }
                IfExpr.Expression = value;
                RaisePropertyChanged();
                RaisePropertyChanged("IfExpr");

            }
        }

        private Expr _IfExpr;
        public Expr IfExpr {
            get => _IfExpr;
            set {
                _IfExpr = value;
                RaisePropertyChanged();
            }
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(IfConstant)}, Expr: {IfExpr}";
        }

        public IList<string> Switches { get; set; } = null;

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            //IfExpr.Reset();
        }

        public new bool Validate() {

            CommonValidate();

            var i = new List<string>();

            if (string.IsNullOrEmpty(IfExpr.Expression)) {
                i.Add("Expression cannot be empty!");
            }

            Switches = ConstantExpression.GetSwitches();
            RaisePropertyChanged("Switches");

            Issues = i;
            return i.Count == 0;
        }

        public string ShowCurrentInfo() {
            try {
                return "Your expression is currently: " + (IfExpr.ValueString.Equals("0") ? "False" : "True");
            } catch (Exception ex) {
                return "Error: " + ex.Message;
            }
        }

    }
}
