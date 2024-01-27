
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
using NINA.Sequencer.Interfaces.Mediator;
using NINA.ViewModel.Sequencer;
using System.Reflection;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Core.Utility;
using System.Linq;
using Accord.Math;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Serialization;
using System.Diagnostics;
using NINA.Core.MyMessageBox;
using System.Runtime.Serialization;
using System.Windows.Navigation;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Call")]
    [ExportMetadata("Description", "Incorporate a template by reference.  Please read the description on the plugin page.")]
    [ExportMetadata("Icon", "BoxClosedSVG")]
    [ExportMetadata("Category", "Powerups (Misc)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class Call : IfCommand, IValidatable {

        static protected ISequenceMediator sequenceMediator;
        static protected ISequenceNavigationVM sequenceNavigationVM;
        static protected TemplateController ninaTemplateController;
        static protected TemplateControllerLite templateController;
        private static SequenceJsonConverter sequenceJsonConverter;
        private static IProfileService profileService;
        private static ISequencerFactory sequencerFactory;

        public static int instanceNumber = 0;

        [ImportingConstructor]
        public Call(ISequenceMediator seqMediator, IProfileService pService) {
            sequenceMediator = seqMediator;
            profileService = pService;
            Instructions = new TemplateContainer();
            Instructions.AttachNewParent(Parent);
            Instructions.PseudoParent = this;
            Instructions.Name = Name;
            Instructions.Icon = Icon;
            Name = Name;
            Id = ++instanceNumber;

            // Get the various NINA components we need
            if (sequenceNavigationVM == null || templateController == null) {
                FieldInfo fi = sequenceMediator.GetType().GetField("sequenceNavigation", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fi != null) {
                    sequenceNavigationVM = (ISequenceNavigationVM)fi.GetValue(sequenceMediator);
                    ISequence2VM s2vm = sequenceNavigationVM.Sequence2VM;
                    if (s2vm != null) {
                        sequencerFactory = s2vm.SequencerFactory;
                        PropertyInfo pi = s2vm.GetType().GetRuntimeProperty("TemplateController");
                        ninaTemplateController = (TemplateController)pi.GetValue(s2vm);
                        fi = ninaTemplateController.GetType().GetField("sequenceJsonConverter", BindingFlags.Instance | BindingFlags.NonPublic);
                        sequenceJsonConverter = (SequenceJsonConverter)fi.GetValue(ninaTemplateController);
                        templateController = new TemplateControllerLite(sequenceJsonConverter, profileService);
                    }
                }
            }
        }

        [OnSerializing]
        public void OnSerializingMethod(StreamingContext context) {
            iInstructions = Instructions;
            Instructions = new TemplateContainer();
        }

        [OnSerialized]
        public void OnSerializedMethod(StreamingContext context) {
            Instructions = iInstructions;
        }

        private IfContainer iInstructions;

        public Call(Call copyMe) : this(sequenceMediator, profileService) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                try {
                    Instructions = (TemplateContainer)copyMe.Instructions.Clone();
                } catch (Exception) {
                    Instructions = copyMe.Instructions.Clone();
                }
                Instructions.PseudoParent = this;
                Instructions.Name = Name;
                Instructions.Icon = Icon;
            }
        }

        public int Id { get; set; }

        public IList<string> issues = new List<string>();
        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        private string iTemplateName = null;
        [JsonProperty]
        public string TemplateName {
            get {
                return iTemplateName;
            }
            set {
                iTemplateName = value;
                RaisePropertyChanged("TemplateName");
            }
        }

        public bool TemplateNameIsTrue {
            get {
                return TemplateName == null;
            }
        }

        public IList<TemplatedSequenceContainer> Templates {
            get {
                lock (TemplateControllerLite.TemplateLock) {
                    return templateController.TBRTemplates;
                }
            }
        }

        private int TemplateCompare(TemplatedSequenceContainer a, TemplatedSequenceContainer b) {
            return String.Compare(a.Container.Name, b.Container.Name);

        }

        public TemplatedSequenceContainer[] SortedTemplates {
            get {
                lock (TemplateControllerLite.TemplateLock) {
                    IList<TemplatedSequenceContainer> l = Templates;
                    TemplatedSequenceContainer[] lCopy = Templates.ToArray();
                    lCopy.Sort(TemplateCompare);
                    return lCopy;
                }
            }
        }

        private TemplatedSequenceContainer selectedTemplate;
        public TemplatedSequenceContainer SelectedTemplate {
            get => selectedTemplate;
            set {
                if (value == null) {
                    value = FindTemplate(TemplateName);
                    if (value == null) {
                        return;
                    }
                }
                selectedTemplate = value;
                if (Instructions.Items.Count > 0) {
                    Instructions.Items.Clear();
                }
                TemplateName = selectedTemplate.Container.Name;
                //Instructions.Items.Add((ISequenceContainer)SelectedTemplate.Container.Clone());
                //foreach (ISequenceItem item in Instructions.Items) {
                //    item.AttachNewParent(Instructions);
                //}
                RaisePropertyChanged("SelectedTemplate");
                RaisePropertyChanged("TemplateNameIsTrue");
                Validate();
            }
        }

        public void Log(string str) {
            //Debug.WriteLine("Instance #" + Id.ToString() + ": " + str);
        }

        public override object Clone() {
            Call clone = new Call(this);
            clone.TemplateName = TemplateName;
 
            if (TemplateName != null && templateController != null) {
                TemplatedSequenceContainer tc = FindTemplate(TemplateName);
                if (tc != null) {
                    SelectedTemplate = tc;
                    TemplateName = tc.Container.Name;
                    RaisePropertyChanged("TemplateNameIsTrue");
                }
            }

            return clone;
        }

        private Stack<string> cycleStack = new Stack<string>();

        private TemplatedSequenceContainer FindTemplate(string name) {

            lock (TemplateControllerLite.TemplateLock) {
                for (int i = 0; i < 4; i++) {
                    try {
                        foreach (var tmp in Templates) {
                            if (tmp.Container.Name.Equals(name)) {
                                return tmp;
                            }
                        }
                    } catch (Exception) {
                        Thread.Sleep(100);
                    }
                }
                return null;
            }
        }

        public override void ResetProgress() {
            base.ResetProgress();
            Instructions.ResetAll();
        }

        public async override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            //Runner runner = new Runner(Instructions, progress, token);
            //await runner.RunConditional();
        }

        public override void AfterParentChanged() {
            // New; provide link up the chain
            Instructions.AttachNewParent(Parent);
            Instructions.PseudoParent = this;
        }

        public override bool Validate() {

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

            if (templateController.Updated) {
                SelectedTemplate = FindTemplate(TemplateName);
                _ = SortedTemplates;
                RaisePropertyChanged("SortedTemplates");
            }

            foreach (ISequenceItem item in Instructions.Items) {
                if (item is IValidatable val) {
                    _ = val.Validate();
                }
            }

            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(Call)}, TemplateName: {TemplateName}";
        }
    }
}
