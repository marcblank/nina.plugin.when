using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System.ComponentModel.Composition;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Constant/Variable Container")]
    [ExportMetadata("Description", "A container for Constant and Variable definitions")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]

    public class CVContainer : SequenceContainer, IValidatable {

        [ImportingConstructor]
        public CVContainer() : base(new SequentialStrategy()) { }

        public CVContainer(CVContainer copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
              }
        }

        public CVContainer(IExecutionStrategy strategy) : base(strategy) {
        }

        public override object Clone() {
            return new CVContainer(this) {
            };
        }  
    }
}
