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
using NINA.Sequencer.Validations;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Constant")]
    [ExportMetadata("Description", "Sets a constant whose numeric value can be used in various instructions")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class SetConstant : Symbol, IValidatable {

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
            IList<string> i = new List<string>();

            if (Identifier.Length == 0 || Definition.Length == 0) {
                i.Add("A name and a value must be specified");
            } else  if (!Regex.IsMatch(Identifier, VALID_SYMBOL)) {
                i.Add("The name of a Constant must be alphanumeric");
            } else if (IsDuplicate) {
                i.Add("The Constant is already defined here; this definition will be ignored.");
            }

            Expr.Validate();

            Issues = i;
            RaisePropertyChanged("Issues");
            return i.Count == 0;
         }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            // Doesn't Execute
            return Task.CompletedTask;
        }

        // Global Constants

        public string GlobalName { get; set; }
        public string GlobalValue { get; set; }
        public string GlobalAll { get; set; }

        public string Dummy;

        private bool allProfiles = true;

        public bool AllProfiles {
            get => allProfiles;
            set {
                if (GlobalName != null) {
                    PropertyInfo pi = WhenPluginObject.GetType().GetProperty(GlobalAll);
                    pi?.SetValue(WhenPluginObject, value, null);
                }
                allProfiles = value;
            }
        }

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
