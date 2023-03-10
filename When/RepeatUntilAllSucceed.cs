using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Sequencer.SequenceItem;
using NINA.Core.Utility;
using System.ComponentModel.Composition;
using NINA.Sequencer.Conditions;
using NINA.Core.Model;
using System.Threading;
using NINA.Core.Enum;
using NINA.Sequencer.Validations;
using System.Runtime.CompilerServices;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Repeat Until All Succeed")]
    [ExportMetadata("Description", "Retry the included instructions until all of them have finished successfully.")]
    [ExportMetadata("Icon", "LoopSVG")]
    [ExportMetadata("Category", "Sequencer")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class RepeatUntilAllSucceed : IfCommand, IValidatable {

        [ImportingConstructor]
        public RepeatUntilAllSucceed() : base() {
            Instructions = new IfContainer();
            Instructions.PseudoParent = this;
        }

        private RepeatUntilAllSucceed(RepeatUntilAllSucceed cloneMe) : this() {
            if (cloneMe != null) {
                CopyMetaData(cloneMe);
                Instructions = (IfContainer)cloneMe.Instructions.Clone();
                Instructions.PseudoParent = this;
            }
        }

        public override object Clone() {
            return new RepeatUntilAllSucceed(this) {
            };
        }
        private string waitTimeExpr = "60";

        [JsonProperty]
        public string WaitTimeExpr {
            get => waitTimeExpr;
            set {
                waitTimeExpr = value;
                ConstantExpression.Evaluate(this, "WaitTimeExpr", "WaitTime", 0);
                RaisePropertyChanged("WaitTimeExpr");
            }
        }

        private double waitTime = 60;

        [JsonProperty]
        public double WaitTime { get => waitTime; set { waitTime = value; RaisePropertyChanged("WaitTime"); } }


        public override void ResetProgress() {
            Status = NINA.Core.Enum.SequenceEntityStatus.CREATED;
            Instructions.ResetProgress();
        }

        public override string ToString() {
            return $"Instruction {nameof(RepeatUntilAllSucceed)}";
        }

        public async override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            int attempts = 1;
            while (true) {
                bool failed = false;
                ISequenceItem failedItem = null;
                foreach (ISequenceItem item in Instructions.Items) {
                    if (item.Status == SequenceEntityStatus.DISABLED) {
                        continue;
                    }
                    try {
                        CancellationTokenSource cts = new CancellationTokenSource();
                        await item.Run(progress, token); // cts.Token);
                        if (cts.IsCancellationRequested) {
                            cts.Dispose();
                            return;
                        }
                        cts.Dispose();
                        if (item.Status == SequenceEntityStatus.FAILED) {
                            // Clear status of all and start over...
                            Logger.Info(item.Name + ": failed, restarting instructions...");
                            failedItem = item;
                            Instructions.ResetProgress();
                            attempts++;
                            failed = true;
                            break;
                        } else {
                            Logger.Info(item.Name + ": ok");
                        }
                    } catch (Exception) {
                        // What?
                    }
                }
                if (!failed) {
                    break;
                }
                if (WaitTime > 0) {
                    await NINA.Core.Utility.CoreUtil.Wait(TimeSpan.FromSeconds(WaitTime), true, token, progress, failedItem.Name + " instruction failed; waiting to repeat");
                }
            }
            Logger.Info("RetryUntilAllSucceed finished after " + attempts + " attempt" + (attempts == 1 ? "." : "s."));
            return;
        }

        private string ValidateTime(double time) {
            if (time >= 0) return String.Empty;
            return "Wait time must be greater than zero!";
        }

        public override bool Validate() {

            if (Instructions.PseudoParent == null) {
                Instructions.PseudoParent = this;
            }

            var i = new List<string>();
            ConstantExpression.Evaluate(this, "WaitTimeExpr", "WaitTime", 0, i);

            string val = ValidateTime(WaitTime);
            if (val != String.Empty) {
                i.Add(val);
            }

            Issues = i;
            return (i.Count == 0);
        }
    }
}