//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//using Newtonsoft.Json;
//using NINA.Core.Model;
//using NINA.Sequencer.Container;
//using NINA.Sequencer.SequenceItem;
//using NINA.Sequencer.Validations;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using System.Text.RegularExpressions;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Windows.Media;
//using NCalc;
//using Castle.Core.Internal;
//using NINA.Core.Utility.Notification;
//using System.Linq;
//using System.Text;
//using Accord.IO;
//using Namotion.Reflection;
//using NINA.Sequencer.DragDrop;
//using NINA.Sequencer.Trigger;
//using System.Windows.Input;
//using NINA.Equipment.Interfaces.Mediator;
//using NINA.Equipment.Equipment.MySafetyMonitor;
//using NINA.Core.Utility;
//using NINA.Sequencer.Conditions;
//using NINA.Sequencer.Interfaces.Mediator;
//using NINA.ViewModel.Sequencer;
//using System.Reflection;
//using NINA.Sequencer;

//namespace WhenPlugin.When {
//    [ExportMetadata("Name", "Template by Reference")]
//    [ExportMetadata("Description", "")]
//    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
//    [ExportMetadata("Category", "When (and If)")]
//    [Export(typeof(ISequenceItem))]
//    [JsonObject(MemberSerialization.OptIn)]
//    public class TemplateByReference : IfCommand, IValidatable {

//        protected ISafetyMonitorMediator safetyMediator;
//        protected ISequenceMediator sequenceMediator;
//        protected ISequenceNavigationVM sequenceNavigationVM;
//        protected TemplateController templateController;


//        [ImportingConstructor]
//        public TemplateByReference (ISafetyMonitorMediator safetyMediator, ISequenceMediator sequenceMediator) {
//            this.safetyMediator = safetyMediator;
//            this.sequenceMediator = sequenceMediator;
//            //IsExpanded = false;

//            Instructions = new SequentialContainer();
//            Instructions.AttachNewParent(Parent);

//            TemplateName = "";

//            // GetField() returns null, so iterate?
//            var fields = sequenceMediator.GetType().GetRuntimeFields();
//            foreach (FieldInfo fi in fields) {
//                if (fi.Name.Equals("sequenceNavigation")) {
//                    sequenceNavigationVM = (ISequenceNavigationVM)fi.GetValue(sequenceMediator);
//                    ISequence2VM s2vm = sequenceNavigationVM.Sequence2VM;
//                    if (s2vm != null) {
//                        PropertyInfo pi = s2vm.GetType().GetRuntimeProperty("TemplateController");
//                        templateController = (TemplateController)pi.GetValue(s2vm);
//                     }
//                }
//            }
//        }

//        public TemplateByReference(TemplateByReference copyMe) : this(copyMe.safetyMediator, copyMe.sequenceMediator) {
//            if (copyMe != null) {
//                CopyMetaData(copyMe);
//                Instructions = (SequentialContainer)copyMe.Instructions.Clone();
//            }
//        }

//        // WE DO NOT WANT TO SERIALIZE THIS
//        public SequentialContainer Instructions;

//        public override object Clone() {
//            return new TemplateByReference(this) {
//            };
//        }

//        private string templateName = "";

//        [JsonProperty]
//        public string TemplateName {
//            get => templateName;
//            set {
//                templateName = value;
//                if (Instructions.Items.IsNullOrEmpty()) {
//                } else {
//                    Instructions.Items.Clear();
//                }
//                RaisePropertyChanged();
//                Validate();
//            }
//        }

//        public new bool Validate() {
//            if (templateController == null) return true;
//            var i = new List<string>();
//            if (Instructions.Items.IsNullOrEmpty()) {
//                foreach (TemplatedSequenceContainer tsc in templateController.Templates) {
//                    if (tsc.Container.Name.Equals(TemplateName)) {
//                        Console.WriteLine("Found template!");
//                        Instructions.Items.Add((ISequenceContainer)tsc.Container.Clone());
//                        foreach (ISequenceItem item in Instructions.Items) {
//                            item.AttachNewParent(Instructions);
//                        }
//                        RaisePropertyChanged("Instructions");
//                        return true;
//                    }
//                }
//                i.Add("Template not found: " + TemplateName);
//            }
//            Issues = i;
//            return i.Count == 0;
//        }

//        public override string ToString() {
//            return $"Category: {Category}, Item: {Name}";
//        }

//        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
//            throw new NotImplementedException();
//        }
//    }
//}

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

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Template by Reference")]
    [ExportMetadata("Description", "Executes an instruction set if the safety monitor indicates that conditions are safe.")]
    [ExportMetadata("Icon", "ShieldSVG")]
    [ExportMetadata("Category", "When (and If)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TemplateByReference : IfCommand, IValidatable {

        protected ISequenceMediator sequenceMediator;
        protected ISequenceNavigationVM sequenceNavigationVM;
        protected TemplateController templateController;

        [ImportingConstructor]
        public TemplateByReference(ISequenceMediator sequenceMediator) {
            this.sequenceMediator = sequenceMediator;
            Instructions = new IfContainer();
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
                Instructions = (IfContainer)copyMe.Instructions.Clone();
                TemplateName = TemplateName;
            }
        }

        public override object Clone() {
            return new TemplateByReference(this) {
            };
        }

        private string templateName;

        [JsonProperty]
        public string TemplateName {
            get { return templateName; }
            set {
                templateName = value;
                RaisePropertyChanged();
                Validate();
            }
        }

        public async override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Runner runner = new Runner(Instructions, null, progress, token);
            await runner.RunConditional();
        }

        public new bool Validate() {
            if (templateController == null) return true;
            var i = new List<string>();
            if (TemplateName.IsNullOrEmpty()) {
                i.Add("A template name must be specified!");
            } else if (Instructions.Items.Count == 0) {
                foreach (TemplatedSequenceContainer tsc in templateController.Templates) {
                    if (tsc.Container.Name.Equals(TemplateName)) {
                        Console.WriteLine("Found template!");
                        Instructions.Items.Add((ISequenceContainer)tsc.Container.Clone());
                        foreach (ISequenceItem item in Instructions.Items) {
                            item.AttachNewParent(Instructions);
                        }
                        RaisePropertyChanged("Instructions");
                        return true;
                    }
                }
                i.Add("The template '" + TemplateName + "' was not found in your template list.");
            }
            Issues = i;
            return i.Count == 0;
        }
    }
}
