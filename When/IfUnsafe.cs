using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using NCalc;
using Castle.Core.Internal;
using NINA.Core.Utility.Notification;
using System.Linq;
using System.Text;
using Accord.IO;
using Namotion.Reflection;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Trigger;
using System.Windows.Input;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Equipment.MySafetyMonitor;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "If Unsafe")]
    [ExportMetadata("Description", "Executes an instruction set if the safety monitor indicates that conditions are unsafe.")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "When")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class IfUnsafe : IfCommand, IValidatable {

        private ISafetyMonitorMediator safetyMediator;

        [ImportingConstructor]
        public IfUnsafe(ISafetyMonitorMediator safetyMediator) {
            Instructions = new IfContainer();
            this.safetyMediator = safetyMediator;
        }
        public IfUnsafe(IfUnsafe copyMe) : this(copyMe.safetyMediator) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Instructions = (IfContainer)copyMe.Instructions.Clone();
            }
        }

        public async override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            while (true) {
                SafetyMonitorInfo info = safetyMediator.GetInfo();
                if (info.IsSafe) {
                    return;
                }

                Notification.ShowSuccess("IfUnsafe true; triggered.");

                Runner runner = new Runner(Instructions, null, progress, token);
                await runner.RunConditional();
                if (runner.ShouldRetry) {
                    runner.ResetProgress();
                    runner.ShouldRetry = false;
                    Notification.ShowSuccess("IfUnsafe; retrying...");
                    continue;
                }

                return;
            }
        }

        public override object Clone() {
            return new IfUnsafe(this) {
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
            return $"Category: {Category}, Item: {nameof(IfUnsafe)}";
        }
    }
}
