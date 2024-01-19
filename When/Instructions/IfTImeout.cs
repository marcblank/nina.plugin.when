using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Sequencer.DragDrop;
using System.Windows.Input;
using NINA.Sequencer.Validations;
using System.Collections;
using System.Collections.Generic;
using NINA.Sequencer.Conditions;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "If Timed Out")]
    [ExportMetadata("Description", "Executes an instruction set if the predicate instruction failed.")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Expressions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class IfTimeout : IfCommand, IValidatable {

        [ImportingConstructor]
        public IfTimeout() {
            Condition = new IfContainer();
            Instructions = new IfContainer();
            DropIntoIfCommand = new GalaSoft.MvvmLight.Command.RelayCommand<DropIntoParameters>(DropIntoCondition);
        }
        public IfTimeout(IfTimeout copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Condition = (IfContainer)copyMe.Condition.Clone();
                Instructions = (IfContainer)copyMe.Instructions.Clone();
            }
        }

        public override object Clone() {
            return new IfTimeout(this) {
            };
        }

        public int Time { get; set; } = 10;

        public ICommand DropIntoIfCommand { get; set; }

        private ConditionWatchdog Watchdog { get; set; }

        public DateTime StartTime {  get; set; }

        public bool TimedOut = false;

        private CancellationTokenSource cts;
        private CancellationTokenSource linkedCts;

        private Task CheckTimer() {
            if (DateTime.Now -  StartTime > TimeSpan.FromSeconds(Time)) {
                linkedCts.Cancel();
                TimedOut = true;
                Notification.ShowWarning("Timed out!");
                Logger.Info("Timeout period over; interrupting...");
            }
            return Task.CompletedTask;
        }
        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            ISequenceItem condition = Condition.Items[0];

            if (condition == null) {
                Status = NINA.Core.Enum.SequenceEntityStatus.FAILED;
                return;
            }

            ConditionWatchdog watch = new ConditionWatchdog(CheckTimer, TimeSpan.FromSeconds(5));
            _ = watch.Start();

            cts = new CancellationTokenSource();
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, token);

            try {
                StartTime = DateTime.Now;
                // Execute the conditional
                condition.Status = NINA.Core.Enum.SequenceEntityStatus.CREATED;
                await condition.Run(progress, linkedCts.Token);

                if (condition.Status != NINA.Core.Enum.SequenceEntityStatus.FAILED) {
                    return;
                }

                Runner runner = new Runner(Instructions, progress, linkedCts.Token);
                await runner.RunConditional();
            } catch (Exception ex) {
                Logger.Info(ex.Message);

            } finally {
                watch.Cancel();
                cts.Dispose();
                linkedCts.Dispose();
            }
        }

        // Allow only ONE instruction to be added to Condition
        public void DropIntoCondition(DropIntoParameters parameters) {
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
