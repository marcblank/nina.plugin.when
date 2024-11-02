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
using System.Text.RegularExpressions;
using NINA.Sequencer.Container;
using NINA.Core.Utility.Converters;
using System.Diagnostics;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "For Each in List")]
    [ExportMetadata("Description", "Iterates over a list of Variables and Expressions, executing the embedded instructions for each")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Fun-ctions)")]
    //[Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class ForEachList : IfCommand, IValidatable {

        [ImportingConstructor]
        public ForEachList() {
            Instructions = new IfContainer();
            Instructions.AttachNewParent(Parent);
            Instructions.PseudoParent = this;
            Instructions.Name = Name;
            Instructions.Icon = Icon;
        }

        public ForEachList(ForEachList copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Instructions = (IfContainer)copyMe.Instructions.Clone();
                Instructions.AttachNewParent(Parent);
                Instructions.PseudoParent = this;
                Instructions.Name = copyMe.Name;
                Instructions.Icon = copyMe.Icon;
                Variable = copyMe.Variable;
                ListExpression = copyMe.ListExpression;
            }
        }

        public override object Clone() {
            return new ForEachList(this) {
            };
        }

        private string variable = "";

        [JsonProperty]
        public string Variable {
            get => variable;
            set {
                if (Parent == null) {
                    //return;
                }
                variable = value;
                RaisePropertyChanged();
            }
        }

        private string listExpression = "";

        [JsonProperty]
        public string ListExpression {
            get => listExpression;
            set {
                if (Parent == null) {
                    //return;
                }
                listExpression = value;
                RaisePropertyChanged();
            }
        }

        private string[] ETokens;
        private string[] VTokens;

        private string ValidateArguments () {
            
            ETokens = ListExpression.Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            VTokens = Variable.Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (ETokens.Length == 0 || VTokens.Length == 0) {
                return "There must be at least one Variable and List Expression";
            }

            foreach (string et in ETokens) {
                string[] etokens = et.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (etokens.Length != VTokens.Length) {
                    return "There must be " + VTokens.Length + " elements in each semicolon-separated Expression; one for each Variable" ;
                }
            }

            return null;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            Logger.Info("ForEach: " + Variable + " in " + ListExpression);
            if (string.IsNullOrEmpty(ListExpression)) {
                Status = SequenceEntityStatus.FAILED;
                return;
            }

            if (ValidateArguments() != null) {
                throw new SequenceEntityFailedException("Syntax error in Variable/List Expression");
            }

            foreach (string var in VTokens) {
                SetVariable v = new SetVariable();
                v.AttachNewParent(Parent);
                v.Variable = var;
                v.Expr.Expression = "0";
                await v.Execute(progress, token);
            }
            
            string[] etokens = ListExpression.Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string[] vtokens = Variable.Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            try {
                for (int e = 0; e < ETokens.Length; e++) {

                    for (int v = 0; v < VTokens.Length; v++) {
                        string var = VTokens[v];

                        string[] exprsList = ETokens[e].Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        string expr = exprsList[v];

                        ResetVariable rv = new ResetVariable();
                        rv.AttachNewParent(Parent);
                        rv.Variable = var;
                        rv.Expr.Expression = expr;
                        Logger.Info("ForEach iteration: Variable = " + var + ", Expression: " + expr);
                        await rv.Execute(progress, token);
                    }

                    Runner runner = new Runner(Instructions, progress, token);
                    await runner.RunConditional();
                    Instructions.ResetAll();
                }
            } catch (Exception ex) {
                Logger.Info("ForEach error: " + ex.Message);
                Status = SequenceEntityStatus.FAILED;
            }
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(ForEachList)}, Variable: {Variable}, List Expression: {ListExpression}";
        }

        public IList<string> Switches { get; set; } = null;

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

        public new bool Validate() {

            CommonValidate();

            var i = new List<string>();
            if (!IsAttachedToRoot()) return true;

            string e = ValidateArguments();
            if (e != null) {
                i.Add(e);
            }

            Switches = Symbol.GetSwitches();
            RaisePropertyChanged("Switches");

            Issues = i;
            return i.Count == 0;
        }
    }
}
