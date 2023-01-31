using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "End Sequence")]
    [ExportMetadata("Description", "Ends the currently running sequence; the End Sequence instructions will run")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "When")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class EndSequence : SequenceItem, IValidatable {
        [ImportingConstructor]
        public EndSequence() {
        }
        public EndSequence(EndSequence copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
            }
        }
        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            // Nothing to do here
            return Task.CompletedTask;
        }

        public override object Clone() {
            return new EndSequence(this) {
            };
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(EndSequence)}";
        }

        public bool Validate() {
            var i = new List<string>();
            Issues = i;
            return i.Count == 0;
        }
    }
}
