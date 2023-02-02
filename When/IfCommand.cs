using Castle.Core.Internal;
using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhenPlugin.When {
    public abstract class IfCommand : SequenceItem, IValidatable {

        [JsonProperty]
        public IfContainer Condition { get; protected set; }
        
        [JsonProperty]
        public IfContainer Instructions { get; protected set; }

        public IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public  static object lockObj = new object();

        public bool HasSpecialChars(string str) {
            return str.Any(ch => !char.IsLetterOrDigit(ch));
        }

        public string RemoveSpecialCharacters(string str) {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str) {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_') {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public override void ResetProgress() {
            base.ResetProgress();
            foreach (ISequenceItem item in Instructions.Items) {
                item.ResetProgress();
            }
            if (Condition != null && !Condition.Items.IsNullOrEmpty()) {
                Condition.Items[0].Status = NINA.Core.Enum.SequenceEntityStatus.CREATED;
            }
        }

        public void Log(string str) {
            Log(str, true);
        }
        
        public void Log(string str, bool success) {
            Logger.Info(str);
            // Notification for debugging...
            //if (success) {
            //    Notification.ShowSuccess(str);
            //} else {
            //    Notification.ShowWarning(str);
            //}
        }

        public virtual bool Validate() {
            var i = new List<string>();
            if (Condition == null) { }
            else if (Condition.Items.IsNullOrEmpty()) {
                i.Add("The instruction to check must not be empty!");
            } else if (Condition.Items[0] is IValidatable val) {
                _ = val.Validate();
            }
            foreach (ISequenceItem item in Instructions.Items) {
                if (item is IValidatable val) {
                    _ = val.Validate();
                }
            }

            Issues = i;
            return i.Count == 0;
        }
    }
}
