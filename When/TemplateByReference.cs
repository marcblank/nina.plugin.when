
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

        static protected ISequenceMediator sequenceMediator;
        static protected ISequenceNavigationVM sequenceNavigationVM;
        static protected TemplateController templateController;

        [ImportingConstructor]
        public TemplateByReference(ISequenceMediator seqMediator) {
            sequenceMediator = seqMediator;
            Instructions = new TemplateContainer();
            Instructions.TBR = this;
            Name = Name;
            
            // Get the various NINA components we need
            if (sequenceNavigationVM == null || templateController == null) {
                FieldInfo fi = sequenceMediator.GetType().GetField("sequenceNavigation", BindingFlags.Instance | BindingFlags.NonPublic); 
                if (fi != null) {
                    sequenceNavigationVM = (ISequenceNavigationVM)fi.GetValue(sequenceMediator);
                    ISequence2VM s2vm = sequenceNavigationVM.Sequence2VM;
                    if (s2vm != null) {
                        PropertyInfo pi = s2vm.GetType().GetRuntimeProperty("TemplateController");
                        templateController = (TemplateController)pi.GetValue(s2vm);
                    }
                }
            }
        }

        public TemplateByReference(TemplateByReference copyMe) : this(sequenceMediator) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Instructions = (TemplateContainer)copyMe.Instructions.Clone();
                Instructions.TBR = this;
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
                RaisePropertyChanged("SelectedTemplate");
                Validate();
            }
        }

        public override object Clone() {
            TemplateByReference clone = new TemplateByReference(this);
            clone.TemplateName = TemplateName;
            if (TemplateName != null && templateController != null) {
                TemplatedSequenceContainer tc = FindTemplate(TemplateName);
                if (tc != null) {
                    SelectedTemplate = tc;
                }
            }
            return clone;
        }

        private TemplatedSequenceContainer FindTemplate(string name) {
            foreach (var tmp in templateController.Templates) {
                if (tmp.Container.Name.Equals(TemplateName)) {
                    Logger.Info("TemplateByReference; found template: " + TemplateName);
                    return tmp;
                }
            }
            Logger.Info("TemplateByReference refers to missing template: " + TemplateName);
            return null;
        }

        public async override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Runner runner = new Runner(Instructions, null, progress, token);
            await runner.RunConditional();
        }

        public bool Validate() {

            if (templateController == null) return true;

            var i = new List<string>();

            if (SelectedTemplate == null && TemplateName == null) {
                i.Add("A template must be selected!");
            } else if (TemplateName != null && SelectedTemplate == null) {
                TemplatedSequenceContainer tc = FindTemplate(TemplateName);
                if (tc != null) {
                    SelectedTemplate = tc;
                } else {
                    i.Add("The specified template '" + TemplateName + "' was not found.");
                }
            } else if (SelectedTemplate == null) {
                i.Add("The specified template '" + TemplateName + "' was not found.");
            }

            Issues = i;
            return i.Count == 0;
        }
    }
}
