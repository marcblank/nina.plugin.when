#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using GalaSoft.MvvmLight.Command;
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
using System.Windows.Input;
using System.Management;
using System.Diagnostics;
using NINA.WPF.Base.Interfaces.Mediator;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "When Becomes Unsafe")]
    [ExportMetadata("Description", "Runs a customizable set of instructions within seconds of an 'Unsafe' condition being recognized.")]
    [ExportMetadata("Icon", "ShieldSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_SafetyMonitor")]
    [Export(typeof(ISequenceTrigger))]

    public class WhenUnsafe : SequenceTrigger, IValidatable {
        protected ISafetyMonitorMediator safetyMediator;
        protected ISequenceMediator sequenceMediator;
        protected ISequenceNavigationVM sequenceNavigationVM;
        private IApplicationStatusMediator applicationStatusMediator;

        [ImportingConstructor]
        public WhenUnsafe (ISafetyMonitorMediator safetyMediator, ISequenceMediator sequenceMediator, IApplicationStatusMediator applicationStatusMediator) {
            this.safetyMediator = safetyMediator;
            this.sequenceMediator = sequenceMediator;
            this.applicationStatusMediator = applicationStatusMediator;
            ConditionWatchdog = new ConditionWatchdog(InterruptWhenUnsafe, TimeSpan.FromSeconds(5));
            Instructions = new IfContainer();
            Instructions.AttachNewParent(Parent);
            Instructions.PseudoParent = this;

            // GetField() returns null, so iterate?
            var fields = sequenceMediator.GetType().GetRuntimeFields();
            foreach (FieldInfo fi in fields) {
                if (fi.Name.Equals("sequenceNavigation")) {
                    sequenceNavigationVM = (ISequenceNavigationVM)fi.GetValue(sequenceMediator);
                }
            }
        }

        protected WhenUnsafe(WhenUnsafe cloneMe) : this(cloneMe.safetyMediator, cloneMe.sequenceMediator, cloneMe.applicationStatusMediator) {
            if (cloneMe != null) {
                CopyMetaData(cloneMe);
                Instructions = (IfContainer)cloneMe.Instructions.Clone();
                Instructions.AttachNewParent(Parent);
                Instructions.PseudoParent = this;
            }
        }

        public static bool inFlight = false;

        public bool InFlight {
            get => inFlight;
            protected set {
                inFlight = value;
                RaisePropertyChanged();
            }
        }


        public ConditionWatchdog ConditionWatchdog { get; set; }

        [JsonProperty]
        public IfContainer Instructions { get; protected set; }

        public override object Clone() {
            return new WhenUnsafe(this);
        }

        private ApplicationStatus _status;

        public ApplicationStatus AppStatus {
            get {
                return _status;
            }
            set {
                _status = value;
                if (string.IsNullOrWhiteSpace(_status.Source)) {
                    _status.Source = Loc.Instance["LblSequence"];
                }

                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(_status);
            }
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

        public bool Check() {
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

        private CancellationTokenSource cts;


        private string startStop = "Stop";
        public string StartStop {
            get {
                return Stopped ? "Restart" : "Pause";
            }
            set { }
        }

        private bool stopped = false;
        public bool Stopped {
            get => stopped;
            set {
                stopped = value;
                RaisePropertyChanged("StartStop");
            }
        }

        private async Task InterruptWhenUnsafe() {
            // Don't even think of it...
            if (Stopped) {
                Logger.Info("WhenUnsafe: Stopped");
                return;
            }

            if (InFlight) return;

            if (!Check() && Parent != null) {
                if (ItemUtility.IsInRootContainer(Parent) && this.Parent.Status == SequenceEntityStatus.RUNNING && this.Status != SequenceEntityStatus.DISABLED) {
                    Logger.Info("Unsafe conditions detected - Interrupting current Instruction Set");
                    sequenceNavigationVM.Sequence2VM.CancelSequenceCommand.Execute(this);
                    Status = SequenceEntityStatus.RUNNING;
                    cts = new CancellationTokenSource();
                    try {
                        // Wait a short time for the sequence to be canceled...
                        Thread.Sleep(1500);
                        Logger.Info("WhenUnsafe: " + "Starting unsafe sequence.");
                        await Execute(new Progress<ApplicationStatus>(p => AppStatus = p), cts.Token);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    } finally {
                        if (!Stopped) {
                            Logger.Info("WhenUnsafe: " + "Finishing unsafe sequence; restarting interrupted sequence.");
                            Status = SequenceEntityStatus.CREATED;
                            InFlight = false;
                            sequenceNavigationVM.Sequence2VM.StartSequenceCommand.Execute(this);
                        }
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

                Logger.Info("WhenUnsafe: Conditions unsafe.");

                // We'll attach ourselves to the sequence that was running
                Instructions.AttachNewParent(Container);
                // And make sure each of our instructions knows it (I'm looking at you, Center and others that inherit coordinates)
                foreach (ISequenceItem item in Instructions.Items) {
                    item.AttachNewParent(Instructions);
                }
                Runner runner = new Runner(Instructions, null, progress, token);
                runner.cts = cts;
                try {
                    // No retries at this point
                    await runner.RunConditional();
                } finally {
                    if (!Stopped) {
                        // Clean up
                        Instructions.AttachNewParent(Parent);
                        foreach (ISequenceItem item in Instructions.Items) {
                            item.AttachNewParent(Instructions);
                        }
                        // Allow this to be run multiple times
                        Instructions.ResetProgress();
                        Status = SequenceEntityStatus.CREATED;
                    }
                }

                return;
            }
        }
 
        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            return Task.CompletedTask; // return Execute(progress, token);
        }

        private GalaSoft.MvvmLight.Command.RelayCommand stopInstructions;

        public ICommand StopInstructions => stopInstructions ??= new GalaSoft.MvvmLight.Command.RelayCommand(PerformStopInstructions);

        private void PerformStopInstructions() {
            if (!Stopped) {
                if (InFlight && cts != null) {
                    cts.Cancel();
                    Stopped = true;
                }
            } else {
                Stopped = false;
                InFlight = false;
                Parent.Status = SequenceEntityStatus.RUNNING;
                _ = InterruptWhenUnsafe();
            }
        }
    }
}