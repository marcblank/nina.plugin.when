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
    [ExportMetadata("Name", "If/Then/Else")]
    [ExportMetadata("Description", "Executes an instruction set if the Expression is True (or 1)")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class IfThenElse : IfCommand, IValidatable, IIfWhenSwitch, ITrueFalse {

        [ImportingConstructor]
        public IfThenElse() {
            IfExpr = new Expr(this);
            Instructions = new IfContainer();
            Instructions.AttachNewParent(Parent);
            Instructions.PseudoParent = this;
            Instructions.Name = Name;
            Instructions.Icon = Icon;
            ElseInstructions = new IfContainer();
            ElseInstructions.AttachNewParent(Parent);
            ElseInstructions.PseudoParent = this;
            ElseInstructions.Name = Name;
            ElseInstructions.Icon = Icon;
        }

        public IfThenElse(IfThenElse copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                IfExpr = new Expr(this, copyMe.IfExpr.Expression);
                Instructions = (IfContainer)copyMe.Instructions.Clone();
                Instructions.AttachNewParent(Parent);
                Instructions.PseudoParent = this;
                Instructions.Name = Name;
                Instructions.Icon = Icon;
                ElseInstructions = (IfContainer)copyMe.ElseInstructions.Clone();
                ElseInstructions.AttachNewParent(Parent);
                ElseInstructions.PseudoParent = this;
                ElseInstructions.Name = Name;
                ElseInstructions.Icon = Icon;
            }
        }

        public override object Clone() {
            return new IfThenElse(this) {
            };
        }

        [JsonProperty]
        public IfContainer ElseInstructions { get; set; }

        public bool Check() {

             return false;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            Logger.Info("If: Execute, Predicate = " + IfExpr.Expression);
            if (string.IsNullOrEmpty(IfExpr.Expression)) {
                Status = SequenceEntityStatus.FAILED;
                return;
            }

            try {
                if (!string.Equals(IfExpr.ValueString, "0", StringComparison.OrdinalIgnoreCase) && (IfExpr.Error == null)) {
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
        public string Predicate {
            get => null;
            set {
                IfExpr.Expression = value;
                RaisePropertyChanged("IfExpr");

            }
        }

        private Expr _IfExpr;
        [JsonProperty]
        public Expr IfExpr {
            get => _IfExpr;
            set {
                _IfExpr = value;
                RaisePropertyChanged();
            }
        }
        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(IfConstant)}, Predicate: {Predicate}";
        }

        public IList<string> Switches { get; set; } = null;

        public new bool Validate() {

            ValidateInstructions(Instructions);
            ValidateInstructions(ElseInstructions);
            IfExpr.Validate();

            var i = new List<string>();

            if (string.IsNullOrEmpty(IfExpr.Expression)) {
                i.Add("Expression cannot be empty!");
            }

            Switches = ConstantExpression.GetSwitches();
            RaisePropertyChanged("Switches");

            Issues = i;
            return i.Count == 0;
        }

    }
}
