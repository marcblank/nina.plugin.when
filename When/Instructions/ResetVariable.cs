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
using System.Diagnostics;
using NINA.Core.Enum;
using NINA.Sequencer;
using NINA.Core.Utility;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Set Variable")]
    [ExportMetadata("Description", "If the variable has been previously defined, its value will become the result of the specified expression")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class ResetVariable : SequenceItem, IValidatable {
        [ImportingConstructor]
        public ResetVariable() {
            Icon = Icon;
            Expr = new Expr(this);
        }
        public ResetVariable(ResetVariable copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                CValueExpr = copyMe.CValueExpr;
                Icon = copyMe.Icon;
                Expr = Expr;
                Expr.Expression = copyMe.Expr.Expression;
                Variable = copyMe.Variable;
            }
        }

        private Expr _Expr = null;

        [JsonProperty]
        public Expr Expr {
            get => _Expr;
            set {
                _Expr = value;
                RaisePropertyChanged();
            }
        }

        private string variable;

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

        private IList<string> issues = new List<string>();

        private static bool Debugging = false;

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }
  
        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
             return Task.CompletedTask;
        }

        public override object Clone() {
            return new ResetVariable(this) {
            };
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

        public override void AfterParentChanged() {
            base.AfterParentChanged();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(ResetVariable)}, Variable: {variable}, Expr: {Expr}";
        }

        public bool Validate() {
            if (!IsAttachedToRoot()) return true;

            var i = new List<string>();

            // Variable has to exist...

            //if (DuplicateName) {
            //    i.Add("Duplicate name in the same instruction set!");
            //}

            // Hmm, what to do?
            
            Issues = i;
            return Issues.Count == 0;
        }

        // Legacy

        [JsonProperty]
        public string CValueExpr {
            get => null;
            set {
                Expr.Expression = value;
                RaisePropertyChanged("Expr.Expression");
            }
        }
    }
}
