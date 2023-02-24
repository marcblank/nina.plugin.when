using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Core.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;
using System.Diagnostics;

namespace WhenPlugin.When {
    public class Runner: SequentialContainer {

        public Runner(SequentialContainer runner, IInstructionResults cmd, IProgress<ApplicationStatus> progress, CancellationToken token) {
            RunInstructions = runner;
            RunInstructions.AttachNewParent(this);
            ConditionalCommand = cmd;
            Progress = progress;
            Token = token;
            ShouldRetry = false;
        }

        public SequentialContainer RunInstructions { get; set; }

        public IInstructionResults ConditionalCommand { get; set; }

        public bool ShouldRetry { get; set; }

        public IProgress<ApplicationStatus> Progress { get; set; }      

        public CancellationToken Token { get; set; }    

        public async Task RunConditional () {
            ShouldRetry = false;
            RunInstructions.Status = SequenceEntityStatus.CREATED;
            Logger.Info("When runner: starting sequence.");
            await RunInstructions.Run(Progress, Token);
            Logger.Info("When runner: finishing sequence.");
        }

        public override void ResetProgress() {
            base.ResetProgress();
            foreach (ISequenceItem item in RunInstructions.Items) {
                if (item is not Retry) {
                    item.ResetProgress();
                } else {
                    item.Status = SequenceEntityStatus.CREATED;
                }
            }
        }
    }
}
