using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "If Results")]
    [ExportMetadata("Description", "Executes an instruction set if the predicate, based on the results of the previous instruction, is true")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]

    public class IfContainer : SequentialContainer, ISequenceContainer, IValidatable {


        public IfContainer() : base() {
        }

        public override IfContainer Clone() {
            IfContainer ic = new IfContainer();
            ic.Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem));
            foreach (var item in ic.Items) {
                item.AttachNewParent(ic);
            }
            return ic;
        }

        private Object lockObj = new Object();

        public new void MoveUp(ISequenceItem item) {
            lock (lockObj) {
                var index = Items.IndexOf(item);
                if (index == 0) {
                    return;
                } else {
                    base.MoveUp(item);
                }
            }
        }
    }
}
