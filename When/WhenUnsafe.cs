#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Core.Enum;
using NINA.Sequencer.Utility;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Container;
using NINA.Core.Model;
using NINA.Sequencer.Conditions;
using Newtonsoft.Json;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Equipment.MySafetyMonitor;
using System.ComponentModel;
using System.Reflection;
using Namotion.Reflection;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.ViewModel.Sequencer;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "When Becomes Unsafe")]
    [ExportMetadata("Description", "Lbl_SequenceCondition_SafetyMonitorCondition_Description")]
    [ExportMetadata("Icon", "ShieldSVG")]
    [ExportMetadata("Category", "When")]
    [Export(typeof(ISequenceTrigger))]

    //public bool Initialized => sequenceNavigation.Initialized;

    public class WhenUnsafe : SequenceTrigger, IValidatable {
        protected ISafetyMonitorMediator safetyMediator;
        protected ISequenceMediator sequenceMediator;
        protected ISequenceNavigationVM sequenceNavigationVM;

        [ImportingConstructor]
        public WhenUnsafe (ISafetyMonitorMediator safetyMediator, ISequenceMediator sequenceMediator) {
            this.safetyMediator = safetyMediator;
            this.sequenceMediator = sequenceMediator;
            ConditionWatchdog = new ConditionWatchdog(InterruptWhenUnsafe, TimeSpan.FromSeconds(5));
            Instructions = new IfContainer();
            Instructions.AttachNewParent(Parent);

            var fields = sequenceMediator.GetType().GetRuntimeFields();
            foreach (FieldInfo fi in fields) {
                if (fi.Name.Equals("sequenceNavigation")) {
                    sequenceNavigationVM = (ISequenceNavigationVM)fi.GetValue(sequenceMediator);
                }
            }
        }

        protected WhenUnsafe(WhenUnsafe cloneMe) : this(cloneMe.safetyMediator, cloneMe.sequenceMediator
            ) {
            if (cloneMe != null) {
                CopyMetaData(cloneMe);
                Instructions = (IfContainer)cloneMe.Instructions.Clone();
                Instructions.AttachNewParent(Parent);
            }
        }

        public static bool InFlight = false;

        public ConditionWatchdog ConditionWatchdog { get; set; }

        [JsonProperty]
        public IfContainer Instructions { get; protected set; }

        public override object Clone() {
            return new WhenUnsafe(this);
        }

        private bool isSafe;

        public bool IsSafe {
            get => isSafe;
            protected set {
                isSafe = value;
                RaisePropertyChanged();
            }
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public bool Validate() {
            var i = new List<string>();
            var info = safetyMediator.GetInfo();

            if (!info.Connected) {
                i.Add(Loc.Instance["LblSafetyMonitorNotConnected"]);
            } else {
                IsSafe = info.IsSafe;
            }

            foreach (ISequenceItem item in Instructions.Items) {
                if (item is IValidatable v) {
                    _ = v.Validate();
                }
            }

            Issues = i;
            return i.Count == 0;
        }
        protected bool IsActive() {
            return ItemUtility.IsInRootContainer(Parent) && Parent.Status == SequenceEntityStatus.RUNNING && Status != SequenceEntityStatus.DISABLED;
        }

        public bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            var info = safetyMediator.GetInfo();
            IsSafe = info.Connected && info.IsSafe;
            if (!IsSafe && IsActive()) {
                Logger.Info($"{nameof(SafetyMonitorCondition)} finished. Status=Unsafe");
            }
            return IsSafe;
        }

        public override void AfterParentChanged() {
            if (Parent == null) {
                SequenceBlockTeardown();
            } else {
                Instructions.AttachNewParent(Parent);
                if (Parent.Status == SequenceEntityStatus.RUNNING) {
                    SequenceBlockInitialize();
                }
            }
        }

        public override void SequenceBlockTeardown() {
            try { ConditionWatchdog?.Cancel(); } catch { }
        }

        public override void SequenceBlockInitialize() {
            ConditionWatchdog?.Start();
        }

        private CancellationTokenSource FindCTS(ISequenceContainer container) {
            var fields = typeof(SequenceContainer).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (FieldInfo f in fields) {
                if (f.Name.Equals("localCTS")) {
                    CancellationTokenSource cts = (CancellationTokenSource)f.GetValue(container);
                    return cts;
                }
            }
            return null;
        }

        private async Task InterruptWhenUnsafe() {
            if (!Check(null, null)) {
                if (this.Parent != null) {
                    if (ItemUtility.IsInRootContainer(Parent) && this.Parent.Status == SequenceEntityStatus.RUNNING && this.Status != SequenceEntityStatus.DISABLED) {
                        Logger.Info("Unsafe conditions detected - Interrupting current Instruction Set");
                        //await Container.Interrupt();
                        //CancellationTokenSource cts = FindCTS(Container);
                        sequenceNavigationVM.Sequence2VM.CancelSequenceCommand.Execute(this);
                        //cts?.Cancel();
                        Status = SequenceEntityStatus.RUNNING;
                        await Execute(null, new CancellationToken());
                        Status = SequenceEntityStatus.CREATED;
                        InFlight = false;
                        sequenceNavigationVM.Sequence2VM.StartSequenceCommand.Execute(this);    
                    }
                }
            }
        }

        public override string ToString() {
            return $"Condition: {nameof(SafetyMonitorCondition)}";
        }

        private ISequenceContainer Container { get; set; }


        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (InFlight) return false;
            Container = previousItem?.Parent;
            if (Container == null) Container = nextItem?.Parent;
            return false;
        }


        public async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            if (InFlight) return;
            InFlight = true;

            while (true) {
                SafetyMonitorInfo info = safetyMediator.GetInfo();
                if (info.IsSafe) {
                    return;
                }

                Notification.ShowSuccess("WhenUnsafe true; triggered.");

                // We'll attach ourselves to the sequence that was running
                Instructions.AttachNewParent(Container);
                // And make sure each of our instructions knows it (I'm looking at you, Center and others that inherit coordinates)
                foreach (ISequenceItem item in Instructions.Items) {
                    item.AttachNewParent(Instructions);
                }
                Runner runner = new Runner(Instructions, null, progress, token);
                try {
                    // No retries at this point
                    await runner.RunConditional();
                } finally {
                    // Clean up
                    Instructions.AttachNewParent(Parent);
                    foreach (ISequenceItem item in Instructions.Items) {
                        item.AttachNewParent(Instructions);
                    }
                    // Allow this to be run multiple times
                    Instructions.ResetProgress();
                    Status = SequenceEntityStatus.CREATED;
                }

                return;
            }
        }
 
        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            return Task.CompletedTask; // return Execute(progress, token);
        }
    }
}