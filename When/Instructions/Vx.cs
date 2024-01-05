using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Variable")]
    [ExportMetadata("Description", "Sets a constant whose numeric value can be used in various instructions")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class SetVariable : Symbol {

        [ImportingConstructor]
        public SetVariable() : base() {
            Name = Name;
            Icon = Icon;
            OriginalExpr = new Expr(this);
        }
        public SetVariable(SetVariable copyMe) : base(copyMe) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Name = copyMe.Name;
                Icon = copyMe.Icon;
            }
        }

        public override object Clone() {
            SetVariable clone = new SetVariable(this);
            clone.Identifier = Identifier;
            clone.Definition = Definition;
            clone.OriginalExpr = new Expr(OriginalExpr);
            return clone;
        }
        private bool iExecuted = false;
        public bool Executed {
            get => iExecuted;
            set {
                iExecuted = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public string OriginalDefinition {
            get => OriginalExpr?.Expression;
            set {
                OriginalExpr.Expression = value;
                RaisePropertyChanged("OriginalExpr");
            }
        }

        private Expr _originalExpr = null;
        public Expr OriginalExpr {
            get => _originalExpr;
            set {
                _originalExpr = value;
                RaisePropertyChanged();
            }
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            OriginalExpr = new Expr(OriginalDefinition, this);
            if (!Executed && Parent != null && Expr != null) {
                Expr.IsExpression = true;
                Expr.Error = "Not evaluated";
            }
        }


        public override string ToString() {
            if (Expr != null) {
                return $"Variable: {Identifier}, Definition: {Definition}, Parent {Parent?.Name} Dirty: {Expr.Dirty}";

            } else {
                return $"Variable: {Identifier}, Definition: {Definition}, Parent {Parent?.Name} Expr: null";
            }
        }

        public override bool Validate() {
            if (!IsAttachedToRoot()) return true;
            IList<string> i = new List<string>();

            if (Identifier.Length == 0 || OriginalDefinition?.Length == 0) {
                i.Add("A name and an initial value must be specified");
            } else if (!Regex.IsMatch(Identifier, VALID_SYMBOL)) {
                i.Add("The name of a Constant must be alphanumeric");
            }

            OriginalExpr.Validate();

            Issues = i;
            return i.Count == 0;
        }

        public override void ResetProgress() {
            base.ResetProgress();
            Executed = false;
            Definition = "";
            Expr.IsExpression = true;
            Expr.Evaluate();
        }


        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Definition = OriginalDefinition;
            Executed = true;
            return Task.CompletedTask;
        }

        // Legacy

        [JsonProperty]
        public string Variable {
            get => null;
            set {
                if (value != null) {
                    Identifier = value;
                }
            }
        }
        
        [JsonProperty]
        public string OValueExpr {
            get => null;
            set {
                if (value != null) {
                    OriginalDefinition = value;
                }
            }
        }

        public string CValue { get; set; }

        public string CValueExpr { get; set; }
    }
}
