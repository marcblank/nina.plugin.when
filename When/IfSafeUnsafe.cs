using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System.ComponentModel.Composition;
using System.Threading;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Core.Utility;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "")]
    [ExportMetadata("Description", "")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class IfSafeUnsafe : IfCommand, IValidatable {

        protected ISafetyMonitorMediator safetyMediator;
        protected bool isSafe = true;

        [ImportingConstructor]
        public IfSafeUnsafe(ISafetyMonitorMediator safetyMediator, bool isSafe) {
            Instructions = new IfContainer();
            this.safetyMediator = safetyMediator;
            this.isSafe = isSafe;
        }
        public IfSafeUnsafe(IfSafeUnsafe copyMe) : this(copyMe.safetyMediator, copyMe.isSafe) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Instructions = (IfContainer)copyMe.Instructions.Clone();
            }
        }

        public async override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            while (true) {
                SafetyMonitorInfo info = safetyMediator.GetInfo();

                if (isSafe && !info.IsSafe) return;
                if (!isSafe && info.IsSafe) return;

                Logger.Info(Name + " true; triggered.");

                Runner runner = new Runner(Instructions, null, progress, token);
                await runner.RunConditional();
                if (runner.ShouldRetry) {
                    runner.ResetProgress();
                    runner.ShouldRetry = false;
                    Logger.Info(Name + "; retrying...");
                    continue;
                }

                return;
            }
        }

        public override void ResetProgress() {
            base.ResetProgress();
            Instructions.ResetProgress();
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
            return $"Category: {Category}, Item: {Name}";
        }
    }
}
