
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using WhenPlugin.When;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.ViewModel.Sequencer;
using System.Reflection;
using NINA.Sequencer;
using System.Runtime.CompilerServices;
using NINA.Sequencer.Container;
using Castle.Core.Internal;
using NINA.Core.Utility;
using NINA.Sequencer.DragDrop;
using System.Windows.Input;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Template by Reference")]
    [ExportMetadata("Description", "Executes an instruction set if the safety monitor indicates that conditions are safe.")]
    [ExportMetadata("Icon", "BoxClosedSVG")]
    [ExportMetadata("Category", "When (and If)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TemplateByReference : SequenceItem, IValidatable {

        protected ISequenceMediator sequenceMediator;
        protected ISequenceNavigationVM sequenceNavigationVM;
        protected TemplateController templateController;

        [ImportingConstructor]
        public TemplateByReference(ISequenceMediator sequenceMediator) {
            this.sequenceMediator = sequenceMediator;
            Instructions = new TemplateContainer();
            Name = Name;
            // GetField() returns null, so iterate?
            var fields = sequenceMediator.GetType().GetRuntimeFields();
            foreach (FieldInfo fi in fields) {
                if (fi.Name.Equals("sequenceNavigation")) {
                    sequenceNavigationVM = (ISequenceNavigationVM)fi.GetValue(sequenceMediator);
                    ISequence2VM s2vm = sequenceNavigationVM.Sequence2VM;
                    if (s2vm != null) {
                        PropertyInfo pi = s2vm.GetType().GetRuntimeProperty("TemplateController");
                        templateController = (TemplateController)pi.GetValue(s2vm);
                    }
                }
            }
        }

        public TemplateByReference(TemplateByReference copyMe) : this(copyMe.sequenceMediator) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Instructions = (TemplateContainer)copyMe.Instructions.Clone();
            }
        }

        public TemplateContainer Instructions { get; protected set; }

        public IList<string> issues = new List<string>();
        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public string TemplateName { get; set; }

        public IList<TemplatedSequenceContainer> Templates { get => templateController.Templates; }

        private TemplatedSequenceContainer selectedTemplate;
        public TemplatedSequenceContainer SelectedTemplate {
            get => selectedTemplate;
            set {
                selectedTemplate = value;
                if (Instructions.Items.Count > 0) {
                    Instructions.Items.Clear();
                }
                TemplateName = selectedTemplate.Container.Name;
                Instructions.Items.Add((ISequenceContainer)SelectedTemplate.Container.Clone());
                foreach (ISequenceItem item in Instructions.Items) {
                    item.AttachNewParent(Instructions);
                }
                RaisePropertyChanged("Instructions");
                Validate();
            }
        }

        public override object Clone() {
            TemplateByReference clone = new TemplateByReference(this);
            clone.TemplateName = TemplateName;
            if (TemplateName != null && templateController != null) {
                foreach (var tmp in templateController.Templates) {
                    if (tmp.Container.Name.Equals(TemplateName)) {
                        Logger.Info("Cloning TemplateByReference; found template " + TemplateName);
                        clone.SelectedTemplate = tmp;
                    }
                }
            }
            return clone;
        }

        public async override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Runner runner = new Runner(Instructions, null, progress, token);
            await runner.RunConditional();
        }

        public bool Validate() {
            if (templateController == null) return true;
            
            var i = new List<string>();

            if (SelectedTemplate == null) {
                if (TemplateName == null && Instructions.Items.Count > 0) {
                    if (Instructions.DroppedTemplate != null) {
                        selectedTemplate = Instructions.DroppedTemplate;
                        TemplateName = selectedTemplate.Container.Name;
                        RaisePropertyChanged("Instructions");
                        RaisePropertyChanged("TemplateName");
                        RaisePropertyChanged("SelectedTemplate");
                    }
                } else if (TemplateName == null) {
                    i.Add("A template must be selected!");
                } else {
                    i.Add("The specified template '" + TemplateName + "' was not found.");
                }
            }

            Issues = i;
            return i.Count == 0;
        }
    }
}
