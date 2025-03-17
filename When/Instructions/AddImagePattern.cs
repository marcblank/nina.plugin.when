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
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility;
using NINA.Sequencer.Logic;
using NINA.Sequencer.Generators;

namespace PowerupsLite.When {
    [ExportMetadata("Name", "Add Image Pattern")]
    [ExportMetadata("Description", "Add an image pattern for file naming")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups Lite")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class AddImagePattern : SequenceItem, IValidatable {

        public static IOptionsVM OptionsVM;

        [ImportingConstructor]
        public AddImagePattern(IOptionsVM options) : base() {
            Name = Name;
            Icon = Icon;
            OptionsVM = options;
        }

        public AddImagePattern(AddImagePattern copyMe) : this(OptionsVM) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Identifier = copyMe.Identifier;
                Expr = new Expression(Expr);
            }
        }

        private Expression expr = new Expression(null, null);
        [JsonProperty]
        public Expression Expr {
            get => expr;
            set {
                expr = value;
                if (value == null) return;
                Expr.Context = this;
                Expr.Type = "String";
                Expr.SymbolBroker = SymbolBroker;
                Expr.Default = 0;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public string Identifier {  get; set; } = string.Empty;

        public override object Clone() {
            return new AddImagePattern(this);
        }

        public override string ToString() {
            return $"AddImagePattern: {Identifier}, Expr: {Expr}";

        }

        public IList<String> Issues {  get; set; }

        public static readonly String VALID_SYMBOL = "^[A-Z]+$";

        private string ImagePatternAdded = String.Empty;

        [JsonProperty]
        public string PatternDescription {  get; set; } = String.Empty;

        public class ImagePatternExpr {

            public ImagePatternExpr (ImagePattern p, Expression e) {
                Pattern = p;
                Expr = e;
            }
            
            public ImagePattern Pattern;
            public Expression Expr;
        }

        public static IList<ImagePatternExpr> ImagePatterns = new List<ImagePatternExpr>();

        public bool Validate() {
            if (!Symbol.IsAttachedToRoot(this)) return true;

            IList<string> i = new List<string>();

            if (Identifier.Length == 0 || Expr?.Definition.Length == 0 || Description.Length == 0) {
                i.Add("A name, value, and description must be specified");
            } else if (!Regex.IsMatch(Identifier, VALID_SYMBOL)) {
                i.Add("The name of an image pattern token must be all uppercase alphabetic characters");
            } else {
                // Create it
                if (ImagePatternAdded.Length == 0) {
                    string desc = PatternDescription;
                    ImagePatterns.Add(new ImagePatternExpr(new ImagePattern("$$" + Identifier + "$$", desc, "Sequencer Powerups"), Expr));
                    OptionsVM.AddImagePattern(new ImagePattern("$$" + Identifier + "$$", desc, "Sequencer Powerups") { Value = "6.66" });
                    ImagePatternAdded = Identifier;
                    Notification.ShowInformation("Image pattern '" + Identifier + "' added");
                }
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

     }
}
