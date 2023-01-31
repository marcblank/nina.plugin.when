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

namespace WhenPlugin.When {
    [ExportMetadata("Name", "If Safe")]
    [ExportMetadata("Description", "Executes an instruction set if the safety monitor indicates that conditions are safe.")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "When")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class IfSafe : IfCommand, IValidatable {

        private ISafetyMonitorMediator safetyMediator;

        [ImportingConstructor]
        public IfSafe(ISafetyMonitorMediator safetyMediator) {
            Instructions = new IfContainer();
            this.safetyMediator = safetyMediator;
        }
        public IfSafe(IfSafe copyMe) : this(copyMe.safetyMediator) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Instructions = (IfContainer)copyMe.Instructions.Clone();
            }
        }

        public async override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
 
            while (true) {
                SafetyMonitorInfo info = safetyMediator.GetInfo();
                if (!info.IsSafe) {
                    return;
                }

                Notification.ShowSuccess("IfSafe true; triggered.");

                Runner runner = new Runner(Instructions, null, progress, token);
                await runner.RunConditional();
                if (runner.ShouldRetry) {
                    runner.ResetProgress();
                    runner.ShouldRetry = false;
                    Notification.ShowSuccess("IfSafe; retrying...");
                   continue;
                }

                return;
            }
        }

        public override object Clone() {
            return new IfSafe(this) {
            };
        }

        public new bool Validate() {
            var i = new List<string>();
            if (safetyMediator == null || !safetyMediator.GetInfo().Connected) {
                i.Add("Safety Monitor must be connected");
            }
            Issues = i;
            return i.Count == 0;       
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(IfSafe)}";
        }
    }
}
