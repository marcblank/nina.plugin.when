using Accord.Diagnostics;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "On Error")]
    [ExportMetadata("Description", "This trigger will be run when a sequence instruction fails.")]
    [ExportMetadata("Icon", "SequenceSVG")]
    [ExportMetadata("Category", "Powerups (Misc)")]
    [Export(typeof(ISequenceTrigger))]
    
    [JsonObject(MemberSerialization.OptIn)]
    public class OnError : SequenceTrigger, IValidatable {

        private GeometryGroup HourglassIcon = (GeometryGroup)Application.Current.Resources["HourglassSVG"];

        [JsonProperty]
        public IfContainer Runner { get; set; }

        public static bool HandlerAdded { get; set; } = false;

        [ImportingConstructor]
        public OnError() {
            Runner = new IfContainer();
        }

        private void AddItem(IfContainer runner, ISequenceItem item) {
            runner.Items.Add(item);
            item.AttachNewParent(runner);
        }

        private OnError(OnError copyMe) {
            CopyMetaData(copyMe);
            Name = copyMe.Name;
            Icon = copyMe.Icon;
            Runner = (IfContainer)copyMe.Runner.Clone();
            Runner.AttachNewParent(Parent);
            Runner.PseudoParent = this;
        }

        public override object Clone() {
            return new OnError(this);
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            ISequenceRootContainer root = ItemUtility.GetRootContainer(Parent);
            if (root != null && HandlerAdded == false) {
                HandlerAdded = true;
                root.FailureEvent += FailureEvent;
            }
        }

        private SequenceEntityFailureEventArgs ErrorArgs { get; set; }

        public Task FailureEvent (object obj, SequenceEntityFailureEventArgs args) {
            ErrorArgs = args;
            return Task.CompletedTask;
        }

        public bool InFlight { get; set; }

        public IList<string> Issues => new List<string>();

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            InFlight = true;
            try {
                await Runner.Run(progress, token);
            } finally {
                InFlight = false;
            }
        }
        
        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            return !InFlight && ErrorArgs != null;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(OnError)}";
        }

        public bool Validate() {
             return true;
        }
    }
}