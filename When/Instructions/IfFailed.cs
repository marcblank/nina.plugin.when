using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Sequencer.DragDrop;
using System.Windows.Input;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "If Fails")]
    [ExportMetadata("Description", "Executes an instruction set if the predicate instruction failed.")]
    [ExportMetadata("Icon", "IfSVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class IfFailed : IfCommand {

        [ImportingConstructor]
        public IfFailed() {
            Condition = new IfContainer();
            Instructions = new IfContainer();
            DropIntoIfCommand = new GalaSoft.MvvmLight.Command.RelayCommand<DropIntoParameters>(DropIntoCondition);
        }
        public IfFailed(IfFailed copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Condition = (IfContainer)copyMe.Condition.Clone();
                Instructions = (IfContainer)copyMe.Instructions.Clone();
            }
        }

        public override object Clone() {
            return new IfFailed(this) {
            };
        }

        public ICommand DropIntoIfCommand { get; set; }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            ISequenceItem condition = Condition.Items[0];

            if (condition == null) {
                Status = NINA.Core.Enum.SequenceEntityStatus.FAILED;
                return;
            }

            while (true) {
                // Execute the conditional
                condition.Status = NINA.Core.Enum.SequenceEntityStatus.CREATED;
                await condition.Run(progress, token);

                if (condition.Status != NINA.Core.Enum.SequenceEntityStatus.FAILED) {
                    return;
                }

                Log("IfFailed - Triggered by: " + condition.Name);

                Runner runner = new Runner(Instructions, progress, token);
                await runner.RunConditional();
                if (runner.ShouldRetry) {
                    Log("IfFailed - Retrying failed instruction: " + condition.Name);
                    runner.ResetProgress();
                    runner.ShouldRetry = false;
                    continue;
                }

                return;
            }
        }

        // Allow only ONE instruction to be added to Condition
        public void DropIntoCondition (DropIntoParameters parameters) {
            lock (lockObj) {
                ISequenceItem item;
                var source = parameters.Source as ISequenceItem;

                if (source.Parent != null && !parameters.Duplicate) {
                    item = source;
                } else {
                    item = (ISequenceItem)source.Clone();
                }

                if (item.Parent != Condition) {
                    item.Parent?.Remove(item);
                    item.AttachNewParent(Condition);
                }

                Condition.Items.Clear();
                Condition.Items.Add(item);
           }
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(IfFailed)}";
        }
    }
}
