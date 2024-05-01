﻿using Newtonsoft.Json;
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
using System.Text;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "If/Then/Else")]
    [ExportMetadata("Description", "Executes an instruction set if the Expression is True (or 1)")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class IfThenElse : IfCommand, IValidatable, ITrueFalse {

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

            Logger.Info("Execute, Predicate: " + IfExpr.Expression);
            if (string.IsNullOrEmpty(IfExpr.Expression)) {
                Status = SequenceEntityStatus.FAILED;
                return;
            }

            Runner runner;
            try {
                if (!string.Equals(IfExpr.ValueString, "0", StringComparison.OrdinalIgnoreCase) && (IfExpr.Error == null)) {
                    Logger.Info("Predicate is true; running Then");
                    runner = new Runner(Instructions, progress, token);
                } else {
                    Logger.Info("Predicate is false; running Else");
                    runner = new Runner(ElseInstructions, progress, token);
                }
                await runner.RunConditional();
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
            return $"Category: {Category}, Item: {nameof(IfThenElse)}, Expr: {IfExpr}";
        }

        public IList<string> Switches { get; set; } = null;

        public override void ResetProgress() {
            base.ResetProgress();
            ElseInstructions.ResetAll();
            foreach (ISequenceItem item in ElseInstructions.Items) {
                item.ResetProgress();
            }
        }

        public override void ResetAll() {
            base.ResetAll();
            ElseInstructions.ResetAll();
        }

        public new bool Validate() {

            var i = new List<string>();

            ValidateInstructions(Instructions);
            ValidateInstructions(ElseInstructions);
            IfExpr.Validate(i);

            Expr.CheckExprError(IfExpr, i);

            Switches = Symbol.GetSwitches();
            RaisePropertyChanged("Switches");

            Issues = i;
            return i.Count == 0;
        }

    }
}
