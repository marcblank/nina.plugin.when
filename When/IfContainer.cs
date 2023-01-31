using Accord.IO;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyPlanetarium;
using NINA.Profile;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "If Results")]
    [ExportMetadata("Description", "Executes an instruction set if the predicate, based on the results of the previous instruction, is true")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]

    public class IfContainer : SequentialContainer {


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
    }
}
