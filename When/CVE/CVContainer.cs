using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Trigger;
using System.Windows.Input;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Constant/Variable Container")]
    [ExportMetadata("Description", "A container for Constant and Variable definitions")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    //[Export(typeof(ISequenceItem))]

    public class CVContainer : IfCommand, IValidatable {

        [ImportingConstructor]
        public CVContainer() {
            Instructions = new IfContainer();
            Instructions.AttachNewParent(Parent);
            Instructions.PseudoParent = this;
            Instructions.Name = Name;
            Instructions.Icon = Icon;
            DropIntoCommand = new GalaSoft.MvvmLight.Command.RelayCommand<DropIntoParameters>(DropInCVContainer);
        }

        public CVContainer(CVContainer copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Instructions = (IfContainer)copyMe.Instructions.Clone();
                Instructions.AttachNewParent(Parent);
                Instructions.PseudoParent = this;
                Instructions.Name = copyMe.Name;
                Instructions.Icon = copyMe.Icon;
                DropIntoCommand = new GalaSoft.MvvmLight.Command.RelayCommand<DropIntoParameters>(DropInCVContainer);
            }
        }

        public ICommand DropIntoCommand { get; set; }

        public override object Clone() {
            return new CVContainer(this) {
            };
        }

        public void DropInCVContainer(DropIntoParameters parameters) {
            lock (lockObj) {
                var source = parameters.Source as ISettable;
                if (source != null) {
                    Instructions.Add((ISequenceItem)source);

                }
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            await Instructions.Execute(progress, token);
            return;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(CVContainer)}";
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
        }

  
    }
}
