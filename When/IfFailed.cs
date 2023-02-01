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
using NINA.Core.Utility;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "If Fails")]
    [ExportMetadata("Description", "Executes an instruction set if the predicate instruction failed.")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    //[ExportMetadata("Category", "When (and If)")]
    //[Export(typeof(ISequenceItem))]
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

                Runner runner = new Runner(Instructions, null, progress, token);
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
