﻿
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
    [ExportMetadata("Name", "Template by Reference")]
    [ExportMetadata("Description", "Incorporate a template by reference.  Please read the description on the plugin page.")]
    [ExportMetadata("Icon", "BoxClosedSVG")]
    [ExportMetadata("Category", "Powerups (Misc)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TemplateByReference : IfCommand, IValidatable {

        static protected ISequenceMediator sequenceMediator;
        static protected ISequenceNavigationVM sequenceNavigationVM;
        static protected TemplateController ninaTemplateController;
        static protected TemplateControllerLite templateController;
        private static SequenceJsonConverter sequenceJsonConverter;
        private static IProfileService profileService;
        private static ISequencerFactory sequencerFactory;

        public static int instanceNumber = 0;

        [ImportingConstructor]
        public TemplateByReference(ISequenceMediator seqMediator, IProfileService pService) {
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
                        Debug.WriteLine("Foo");

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

        public TemplateByReference(TemplateByReference copyMe) : this(sequenceMediator, profileService) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                try {
                    Instructions = (TemplateContainer)copyMe.Instructions.Clone();
                } catch (Exception ex) {
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
        
        public IList<TemplatedSequenceContainer> Templates { get => templateController.Templates; }

        private int TemplateCompare (TemplatedSequenceContainer a, TemplatedSequenceContainer b) {
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
                    //RaisePropertyChanged("SelectedTemplate");
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
                Instructions.Items.Add((ISequenceContainer)SelectedTemplate.Container.Clone());
                foreach (ISequenceItem item in Instructions.Items) {
                    item.AttachNewParent(Instructions);
                }
                RaisePropertyChanged("SelectedTemplate");
                RaisePropertyChanged("TemplateNameIsTrue");
                Validate();
            }
        }

        public void Log(string str) {
            //Debug.WriteLine("Instance #" + Id.ToString() + ": " + str);
        }

        public override object Clone() {
            if (TemplateName != null && cycleStack.Contains(TemplateName)) {
                Notification.ShowError("The template '" + TemplateName + "' is recursive.  Please don't do that.");
                TemplateName = "{Error}";
                throw new Exception("Recursive template");
            }
            if (TemplateName != null) {
                cycleStack.Push(TemplateName);
            }

            TemplateByReference clone = new TemplateByReference(this);
            clone.TemplateName = TemplateName;
            if (TemplateName != null && templateController != null) {
                TemplatedSequenceContainer tc = FindTemplate(TemplateName);
                if (tc != null) {
                    SelectedTemplate = tc;
                    TemplateName = tc.Container.Name;
                    RaisePropertyChanged("TemplateNameIsTrue");
                }
            }
            if (TemplateName != null) {
                cycleStack.Pop();
            }

            Log("Clone #" + clone.Id + " returned, TemplateName = " + TemplateName);
            return clone;
        }

        private Stack<string> cycleStack = new Stack<string>();

        private TemplatedSequenceContainer FindTemplate(string name) {

            lock (TemplateControllerLite.TemplateLock) {
                for (int i = 0; i < 4; i++) {
                    try {
                        foreach (var tmp in templateController.Templates) {
                            if (tmp.Container.Name.Equals(name)) {
                                //Logger.Info("TemplateByReference; found template: " + TemplateName);
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
            Runner runner = new Runner(Instructions, null, progress, token);
            await runner.RunConditional();
        }

        
        private void UpdateChangedTemplates(string name) {
            if (name == null) return;
            ISequenceContainer p = Parent;
            while (p != null) {
                if (p is SequenceRootContainer root) {
                    try {
                        UpdateChangedTemplate(root, name.Split('.')[0]);
                    } catch (Exception ex) {
                        Logger.Warning("Exception trying to parse template file name: " + name + " Error: " + ex.Message);
                    }
                    return;
                }
                p = p.Parent;
            }
            return;
        }

        private void UpdateChangedTemplate(ISequenceContainer p, string name) {
            foreach (ISequenceItem item in p.Items) {
                if (item is ISequenceContainer sc) {
                    UpdateChangedTemplate(sc, name);
                } else if (item is TemplateByReference tbr) {
                    if (tbr.TemplateName.Equals(name)) {
                        // Update instruction set if user wants
                        if (MyMessageBox.Show("An instruction set named '" + tbr.Parent.Name + "' includes a reference to template '" + name + "', which has just been changed.  Do you want this instruction set to be updated as well?", "Update instruction set?", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                            tbr.SelectedTemplate = FindTemplate(name);
                            tbr.Log("Updated due to '" + name + "' changed.");
                            Notification.ShowSuccess("The instruction set '" + tbr.Parent.Name + "' has been updated.");
                        }
                    }
                }
            }
        }

        public override void AfterParentChanged() {
            // New; provide link up the chain
            Instructions.AttachNewParent(Parent);
            Instructions.PseudoParent = this;
        }
        
        public bool Validate() {

            if (templateController == null) return true;

            Log("Validate");

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
                UpdateChangedTemplates(templateController.UpdatedFile);
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
            return $"Category: {Category}, Item: {nameof(TemplateByReference)}, TemplateName: {TemplateName}";
        }
    }
}
