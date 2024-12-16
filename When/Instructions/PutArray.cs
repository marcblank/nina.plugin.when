using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using NINA.Sequencer.Validations;
using System.Collections.Generic;
using System.Reflection;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Put into Array")]
    [ExportMetadata("Description", "Puts a value into an Array at the specified index")]
    [ExportMetadata("Icon", "ArraySVG")]
    [ExportMetadata("Category", "Powerups (Fun-ctions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class PutArray : SequenceItem, IValidatable {

        [ImportingConstructor]
        public PutArray() : base() {
            Name = Name;
            Icon = Icon;
            IExpr = new Expr(this);
            VExpr = new Expr(this);
        }

        public PutArray(PutArray copyMe) : base(copyMe) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Name = copyMe.Name;
                Icon = copyMe.Icon;
            }
        }

        public override object Clone() {
            PutArray clone = new PutArray(this);
            clone.Identifier = Identifier;
            clone.IExpr = new Expr(clone, this.IExpr.Expression, "Any");
            clone.VExpr = new Expr(clone, this.VExpr.Expression);
            return clone;
        }

        private Expr _IExpr = null;

        [JsonProperty]
        public Expr IExpr {
            get => _IExpr;
            set {
                _IExpr = value;
                RaisePropertyChanged();
            }
        }

        private Expr _VExpr = null;

        [JsonProperty]
        public Expr VExpr {
            get => _VExpr;
            set {
                _VExpr = value;
                RaisePropertyChanged();
            }
        }

        public static readonly String VALID_SYMBOL = "^[a-zA-Z][a-zA-Z0-9-+_]*$";

        private string _Identifier = "";
        
        [JsonProperty]
        public string Identifier {
            get {
                return _Identifier;
            }
            set {
                if (value.Length > 0) {
                    value = value.Trim();
                }
                _Identifier = value;
            }
        }

        public override string ToString() {
            return $"Put Array: {Identifier} at {IExpr.Value}, value {VExpr.Value}";
        }

        private IList<string> issues = new List<string>();
        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public bool Validate() {
            IList<string> i = new List<string>();

            if (Identifier.Length == 0) {
                i.Add("A name for the Array must be specified");
            } else if (!Regex.IsMatch(Identifier, VALID_SYMBOL)) {
                i.Add("The name of an Array must be alphanumeric");
            //} else if (!Symbol.Arrays.ContainsKey(Identifier)) {
            //    i.Add("The Array named '" + Identifier + "' has not been initialized");
            } else if (IExpr != null && IExpr.Expression != null && IExpr.Expression.Length == 0) {
                i.Add("The Array index must be specified");
            }

            Expr.AddExprIssues(i, IExpr, VExpr);

            Issues = i;
            RaisePropertyChanged("Issues");
            return i.Count == 0;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Symbol.Array arr;
            if (!Symbol.Arrays.TryGetValue(Identifier, out arr)) {
                throw new SequenceEntityFailedException("The Array named '" + Identifier + " has not been initialized");
            }
            if (!arr.TryAdd(IExpr.ValueString, VExpr.Value)) {
                throw new SequenceEntityFailedException("Adding Array value at index " + IExpr.Value + " failed?");
            }
            return Task.CompletedTask;
        }
    }
}
