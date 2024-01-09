using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System.ComponentModel.Composition;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Core.Locale;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility.Notification;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer;
using NINA.Profile;
using NINA.Sequencer.Mediator;
using NINA.Sequencer.Serialization;
using NINA.ViewModel.Sequencer;
using System.Reflection;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Interfaces.Mediator;
using System.Linq;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Constant/Variable Container")]
    [ExportMetadata("Description", "A container for Constant and Variable definitions")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    //[Export(typeof(ISequenceItem))]
    //[Export(typeof(ISequenceContainer))]

    public class CVContainer : SequenceContainer, IValidatable {

        static protected ISequenceMediator sequenceMediator;
        static protected ISequenceNavigationVM sequenceNavigationVM;
        static protected TemplateController ninaTemplateController;
        private static IProfileService profileService;
        private static ISequencerFactory sequencerFactory;

        [ImportingConstructor]
        public CVContainer(ISequenceMediator seqMediator) : base(new SequentialStrategy()) {
            sequenceMediator = seqMediator;
            if (sequenceNavigationVM == null) {
                FieldInfo fi = sequenceMediator.GetType().GetField("sequenceNavigation", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fi != null) {
                    sequenceNavigationVM = (ISequenceNavigationVM)fi.GetValue(sequenceMediator);
                    ISequence2VM s2vm = sequenceNavigationVM.Sequence2VM;
                    if (s2vm != null) {
                        sequencerFactory = s2vm.SequencerFactory;
                        PropertyInfo pi = s2vm.GetType().GetRuntimeProperty("TemplateController");
                        ninaTemplateController = (TemplateController)pi.GetValue(s2vm);
                    }
                }
            }
        }

        public CVContainer(CVContainer copyMe) : this(sequenceMediator) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
              }
        }

        public CVContainer(IExecutionStrategy strategy) : base(strategy) {
        }

        public override object Clone() {
            return new CVContainer(this) {
                Name = Name
            };
        }

        private void AddTemplate(object o) {
            ISequenceContainer clonedContainer = ((o as DropIntoParameters).Source as ISequenceContainer).Clone() as ISequenceContainer;
            if (clonedContainer == null || clonedContainer is ISequenceRootContainer || clonedContainer is IImmutableContainer) return;
            clonedContainer.AttachNewParent(null);
            clonedContainer.ResetAll();
            if (clonedContainer is DeepSkyObjectContainer dsoContainer) {
                dsoContainer.ExposureInfoList.Clear();
            }

            bool addTemplate = true;
            var templateExists = ninaTemplateController.UserTemplates.Any(t => t.Container.Name == clonedContainer.Name);
            if (templateExists) {
                var result = MyMessageBox.Show(string.Format(Loc.Instance["LblTemplate_OverwriteTemplateMessageBox_Text"], clonedContainer.Name),
                    Loc.Instance["LblTemplate_OverwriteTemplateMessageBox_Caption"], System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
                addTemplate = result == System.Windows.MessageBoxResult.OK;
            }

            if (addTemplate) {
                ninaTemplateController.AddNewUserTemplate(clonedContainer);
                if (templateExists) {
                    Notification.ShowSuccess(string.Format(Loc.Instance["LblTemplate_Updated"], clonedContainer.Name));
                } else {
                    Notification.ShowSuccess(string.Format(Loc.Instance["LblTemplate_Created"], clonedContainer.Name));
                }
            }
        }

    }
}
