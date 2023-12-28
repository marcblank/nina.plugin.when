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

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Define Constant")]
    [ExportMetadata("Description", "Sets a constant whose numeric value can be used in various instructions")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (CV)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class SetConstant : Symbol {

        [ImportingConstructor]
        public SetConstant() : base() {
            Name = Name;
            Icon = Icon;
        }
        public SetConstant(SetConstant copyMe) : base(copyMe) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Name = copyMe.Name;
                Icon = copyMe.Icon;               
            }
        }

        public override object Clone() {
            return new SetConstant(this) {
                Identifier = Identifier,
                Definition = Definition,
                Expr = Expr
            };
        }

        public override string ToString() {
            if (Expr != null) {
                return $"Constant: {Identifier}, Definition: {Definition}, Parent {Parent?.Name} Dirty: {Expr.Dirty}";

            } else {
                return $"Constant: {Identifier}, Definition: {Definition}, Parent {Parent?.Name} Expr: null";
            }
        }

        public override bool Validate() {
            if (!IsAttachedToRoot()) return true;
            Issues.Clear();

            if (Identifier.Length == 0 || Definition.Length == 0) {
                Issues.Add("A name and a value must be specified");
                return false;
            }

            if (!Regex.IsMatch(Identifier, "[a-zA-Z]^[a-zA-Z0-9]*$")) {
  //              Issues.Add("The name of a Constant must be alphanumeric");
   //             return false;
            }

            if (Expr.Error != null) {
                Expr.Evaluate();
            }

            return true;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            // Doesn't Execute
            return Task.CompletedTask;
        }

        // DEBUGGING
        public void WriteSymbols() {
            Debug.WriteLine(this);
            Debug.WriteLine(Expr);
            Debug.WriteLine("=-----------");
            ShowSymbols();
        }

        private GalaSoft.MvvmLight.Command.RelayCommand postInstructions;
        public ICommand SendInstruction => postInstructions ??= new GalaSoft.MvvmLight.Command.RelayCommand(WriteSymbols);

        // Legacy

        [JsonProperty]
        public string Constant {
            get => null;
            set {
                if (value != null) Identifier = value;
            }
        }
        
        [JsonProperty]
        public string CValueExpr {
            get => null;
            set {
                if (value != null) Definition = value;
            }
        }


    }
}
