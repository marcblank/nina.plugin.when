using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using NINA.Sequencer.Container;
using System.Text;
using NINA.Core.Utility;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Constant")]
    [ExportMetadata("Description", "Sets a constant whose numeric value can be used in various instructions")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (CV)")]
    //[Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class Cx : Symbol {

        [ImportingConstructor]
        public Cx() : base() {
            Name = Name;
            Icon = Icon;
        }
        public Cx(Cx copyMe) : base(copyMe) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Name = copyMe.Name;
                Icon = copyMe.Icon;               
            }
        }

        public override object Clone() {
            return new Cx(this) {
                Identifier = Identifier,
                Definition = Definition,
                Expr = Expr
            };
        }

        public override string ToString() {
            return $"Cx: Identifier: {Identifier}, Definition: {Definition}, Parent {Parent?.Name}";
        }

        public override bool Validate() {
            if (!IsAttachedToRoot()) return true;
            Issues.Clear();
            if (Identifier.Length == 0 || Definition.Length == 0) {
                Issues.Add("A name and a value must be specified");
                return false;
            }
            // For now...
            Expr.Evaluate();
            return true;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            // Doesn't Execute
            return Task.CompletedTask;
        }
    }
}
