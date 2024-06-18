using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Sequencer.DragDrop;
using System.Windows.Input;
using System.Text.RegularExpressions;
using NINA.Core.Utility;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "Send via Ground Station")]
    [ExportMetadata("Description", "Send a message via Ground Station, including Powerups Expressions.")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [ExportMetadata("Category", "Powerups (Fun-ctions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class GSSend : IfCommand {

        [ImportingConstructor]
        public GSSend() {
            Condition = new IfContainer();
            Instructions = new IfContainer();
            DropIntoIfCommand = new GalaSoft.MvvmLight.Command.RelayCommand<DropIntoParameters>(DropIntoCondition);
        }
        public GSSend(GSSend copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Condition = (IfContainer)copyMe.Condition.Clone();
                Instructions = (IfContainer)copyMe.Instructions.Clone();
            }
        }

        public override object Clone() {
            return new GSSend(this) {
            };
        }

        public ICommand DropIntoIfCommand { get; set; }

        public string ProcessedScriptError = null;

        public string ProcessedScript(string message) {
            string value = message;
            RaisePropertyChanged();
            if (value != null) {
                while (true) {
                    string toReplace = Regex.Match(value, @"\{([^\}]+)\}").Groups[1].Value;
                    if (toReplace.Length == 0) break;
                    Expr ex = new Expr(this, toReplace);
                    ProcessedScriptError = null;
                    if (ex.Error != null) {
                        ProcessedScriptError = ex.Error;
                        Logger.Warning("Send via Ground Station, error processing script, " + ex.Error);
                        return "Error";
                    }
                    value = value.Replace("{" + toReplace + "}", ex.ValueString);
                }
            }
            RaisePropertyChanged("ProcessedScriptAnnotated");
            return value;
        }


        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            ISequenceItem condition = Condition.Items[0];

            if (condition == null) {
                Status = NINA.Core.Enum.SequenceEntityStatus.FAILED;
                return;
            }

            // Execute the conditional
            condition.Status = NINA.Core.Enum.SequenceEntityStatus.CREATED;

            var messageProperty = condition.GetType().GetProperty("Message");
            if (messageProperty == null) {
                throw new SequenceEntityFailedException("Not a Ground Station instruction?");
            }
            string message = (string)messageProperty.GetValue(condition);
            var processedMessage = ProcessedScript(message);
            if (ProcessedScriptError != null) {
                throw new SequenceEntityFailedException("Error processing message for Ground Station: " + ProcessedScriptError);
            }
            messageProperty.SetValue(condition, processedMessage, null);
            await condition.Run(progress, token);
            messageProperty.SetValue(condition, message, null);
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

        public override bool Validate() {
            Issues.Clear();
            if (Condition == null || Condition.Items.Count == 0) {
                issues.Add("There must be a Ground Station instruction included in this instruction");
            } else {
                string name = Condition.Items[0].Name;
                if (!name.StartsWith("Send to") && !name.Equals("Send Email")) {
                    issues.Add("The Ground Station instruction must be \"Send to xxx\" or \"Send email\"");
                }
             }
            return issues.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(GSSend)}";
        }
    }
}
