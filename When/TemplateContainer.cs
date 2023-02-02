using Accord.IO;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyPlanetarium;
using NINA.Profile;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
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
using System.Windows.Input;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "If Results")]
    [ExportMetadata("Description", "Executes an instruction set if the predicate, based on the results of the previous instruction, is true")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    //[Export(typeof(ISequenceItem))]
    //[Export(typeof(ISequenceContainer))]
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]

    public class TemplateContainer : SequentialContainer {

        public TemplateContainer() : base() {
            DropIntoTemplateCommand = new GalaSoft.MvvmLight.Command.RelayCommand<DropIntoParameters>(DropIntoTemplate);
        }

        public override TemplateContainer Clone() {
            TemplateContainer ic = new TemplateContainer();
            ic.Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem));
            foreach (var item in ic.Items) {
                item.AttachNewParent(ic);
            }
            return ic;
        }

        private object lockObj = new object();
        
        public TemplatedSequenceContainer DroppedTemplate { get; set; }

        public ICommand DropIntoTemplateCommand { get; set; }

        // Allow only ONE instruction to be added to Instructions
        public void DropIntoTemplate(DropIntoParameters parameters) {
            lock (lockObj) {
                ISequenceContainer item;
                var source = parameters.Source as TemplatedSequenceContainer;
                DroppedTemplate = source;

                if (source.Parent != null && !parameters.Duplicate) {
                    item = source.Container;
                } else {
                    item = (ISequenceContainer)source.Container.Clone();
                }

                ISequenceItem seq = item as ISequenceItem;

                if (seq.Parent != this) {
                    seq.Parent?.Remove(seq);
                    seq.AttachNewParent(this);
                }

                Items.Clear();
                Items.Add(seq);
            }
        }

    }
}
