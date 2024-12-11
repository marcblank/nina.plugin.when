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
    [ExportMetadata("Name", "Initialize Array")]
    [ExportMetadata("Description", "Creates or re-initializes an Array")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Fun-ctions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class InitializeArray : SequenceItem, IValidatable {

        [ImportingConstructor]
        public InitializeArray() : base() {
            Name = Name;
            Icon = Icon;
        }
        public InitializeArray(InitializeArray copyMe) : base(copyMe) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Name = copyMe.Name;
                Icon = copyMe.Icon;
            }
        }

        public override object Clone() {
            InitializeArray clone = new InitializeArray(this);

            clone.Identifier = Identifier;
            return clone;
        }

        public static readonly String VALID_SYMBOL = "^[a-zA-Z][a-zA-Z0-9-+_]*$";

        [JsonProperty]
        public string Identifier { get; set; }

        public override string ToString() {
                return $"Initialize Array: {Identifier}";
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
            }

            Issues = i;
            RaisePropertyChanged("Issues");
            return i.Count == 0;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Symbol.Array arr;
            if (Symbol.Arrays.TryGetValue(Identifier, out arr)) {
                arr.Clear();
            } else {
                Symbol.Arrays.TryAdd(Identifier, new Symbol.Array());
            }
            return Task.CompletedTask;
        }
    }
}
