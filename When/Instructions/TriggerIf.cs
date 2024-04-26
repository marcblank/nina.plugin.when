using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Trigger If")]
    [ExportMetadata("Description", "The specified trigger will only be active when the Expression is true.")]
    [ExportMetadata("Icon", "WandSVG")]
    [ExportMetadata("Category", "Powerups (Misc)")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TriggerIf : SequenceTrigger, IValidatable {


        [ImportingConstructor]
        public TriggerIf() {
            DropIntoDIYTriggersCommand = new GalaSoft.MvvmLight.Command.RelayCommand<DropIntoParameters>(DropInSequenceTrigger);
            IfExpr = new Expr(this);
        }

        public override bool AllowMultiplePerSet => true;

        public ICommand DropIntoDIYTriggersCommand { get; set; }

        public override object Clone() {
            var clone = new TriggerIf() {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                TriggerRunner = (SequentialContainer)TriggerRunner.Clone(),
            };

            IfExpr = new Expr(clone, IfExpr.Expression);
            return clone;
        }

        private static object lockObj = new object();
        public bool InFlight { get; set; }

        private Expr _IfExpr;

        [JsonProperty]
        public Expr IfExpr {
            get => _IfExpr;
            set {
                _IfExpr = value;
                RaisePropertyChanged();
            }
        }
        private void DropInSequenceTrigger(DropIntoParameters parameters) {
            lock (lockObj) {
                ISequenceTrigger item;
                var source = parameters.Source as ISequenceTrigger;

                if (source.Parent != null && !parameters.Duplicate) {
                    item = source;
                } else {
                    item = (ISequenceTrigger)source.Clone();
                }

                if (item.Parent != TriggerRunner) {
                    item.Parent?.Remove(item);
                    item.AttachNewParent(TriggerRunner);
                }

                TriggerRunner.Triggers.Clear();
                TriggerRunner.Triggers.Add(item);
            }
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = ImmutableList.CreateRange(value);
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// The actual running logic for when the trigger should run
        /// </summary>
        /// <param name="context"></param>
        /// <param name="progress"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            InFlight = true;
            TriggerRunner.AttachNewParent(context);

            try {
                await TriggerRunner.Run(progress, token);
            } finally {
                InFlight = false;
                TriggerRunner.Parent?.Remove(TriggerRunner);
                TriggerRunner.AttachNewParent(Parent);
            }
        }
        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (InFlight) return false;
            if (TriggerRunner.Triggers.FirstOrDefault() == null) return false;
            return TriggerRunner.Triggers.FirstOrDefault().ShouldTrigger(previousItem, nextItem);
        }

        public override bool ShouldTriggerAfter(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (InFlight) return false;
            if (TriggerRunner.Triggers.FirstOrDefault() == null) return false;
            return TriggerRunner.Triggers.FirstOrDefault().ShouldTriggerAfter(previousItem, nextItem);
        }

        // Per Nick Holland
        public override void SequenceBlockInitialize() {
            if (!InFlight && TriggerRunner.Triggers.FirstOrDefault() != null)
                TriggerRunner.Triggers.FirstOrDefault().SequenceBlockInitialize();
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            foreach (ISequenceTrigger item in TriggerRunner.Triggers) {
                if (item.Parent == null) item.AttachNewParent(TriggerRunner);
            }
            foreach (ISequenceItem item in TriggerRunner.Items) {
                if (item.Parent == null) item.AttachNewParent(TriggerRunner);
            }
            TriggerRunner.AttachNewParent(Parent);
            IfExpr.Validate();
        }
        public virtual bool Validate() {
            // Validate the Items (this will update their status)
            if (TriggerRunner == null) return true;
            foreach (ISequenceTrigger item in TriggerRunner.Triggers) {
                if (item is IValidatable vitem) {
                    _ = vitem.Validate();
                }
            }
            foreach (ISequenceItem item in TriggerRunner.Items) {
                if (item is IValidatable vitem) {
                    _ = vitem.Validate();
                }
            }
            IfExpr.Validate();
            return true;
        }
        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(DIYTrigger)}";
        }
    }
}