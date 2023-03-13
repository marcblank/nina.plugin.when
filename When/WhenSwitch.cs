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
using Castle.Core.Internal;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Sequencer;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "When Switch/Weather")]
    [ExportMetadata("Description", "Runs a customizable set of instructions within seconds seconds of your specified expression being satisfied.")]
    [ExportMetadata("Icon", "SwitchesSVG")]
    [ExportMetadata("Category", "Switch")]
    [Export(typeof(ISequenceTrigger))]

    public class WhenSwitch : SequenceTrigger, IValidatable, IIfWhenSwitch {
        private ISwitchMediator switchMediator;
        private IWeatherDataMediator weatherMediator;
        protected ISequenceMediator sequenceMediator;
        protected ISequenceNavigationVM sequenceNavigationVM;
        private IApplicationStatusMediator applicationStatusMediator;

        [ImportingConstructor]
        public WhenSwitch(ISwitchMediator switchMediator, IWeatherDataMediator weatherMediator, ISequenceMediator sequenceMediator, IApplicationStatusMediator applicationStatusMediator) {
            this.switchMediator = switchMediator;
            this.weatherMediator = weatherMediator;
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

        protected WhenSwitch(WhenSwitch cloneMe) : this(cloneMe.switchMediator, cloneMe.weatherMediator, cloneMe.sequenceMediator, cloneMe.applicationStatusMediator) {
            if (cloneMe != null) {
                CopyMetaData(cloneMe);
                Instructions = (IfContainer)cloneMe.Instructions.Clone();
                Instructions.AttachNewParent(Parent);
                Instructions.PseudoParent = this;
                Predicate = cloneMe.Predicate;
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
        
        [JsonProperty]
        public bool OnceOnly { get; set; } = true;

 
        public ConditionWatchdog ConditionWatchdog { get; set; }

        [JsonProperty]
        public IfContainer Instructions { get; protected set; }

        public override object Clone() {
            return new WhenSwitch(this);
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

        private IList<string> switches = new List<string>();

        public IList<string> Switches {
            get => switches;
            set {
                switches = value;
                RaisePropertyChanged();
            }
        }

        private string iPredicate = "";

        [JsonProperty]
        public string Predicate {
            get => iPredicate;
            set {
                iPredicate = value;
                RaisePropertyChanged("Predicate");
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

            if (Instructions.PseudoParent == null) {
                Instructions.PseudoParent = this;
            }

            if (Predicate.IsNullOrEmpty()) {
                i.Add("Expression cannot be empty!");
            }

            try {
                IfWhenSwitch.EvaluatePredicate(Predicate, switchMediator, weatherMediator);
            } catch (Exception ex) {
                i.Add("Error in expression: " + ex.Message);
            }

            if (switchMediator.GetInfo().Connected || weatherMediator.GetInfo().Connected) { }
            else {
                i.Add("No switches or weather devices are connected");
                Issues = i;
                return false;
            }

            Switches = IfWhenSwitch.GetSwitchList(switchMediator, weatherMediator);
            RaisePropertyChanged("Switches");

            foreach (ISequenceItem item in Instructions.Items) {
                if (item is IValidatable val) {
                    _ = val.Validate();
                }
            }

            Issues = i;
            return i.Count == 0;
        }

        protected bool IsActive() {
            return ItemUtility.IsInRootContainer(Parent) && Parent.Status == SequenceEntityStatus.RUNNING && Status != SequenceEntityStatus.DISABLED;
        }

        public bool Check() {
            object result = IfWhenSwitch.EvaluatePredicate(Predicate, switchMediator, weatherMediator);
            if (result == null) {
                return true;
            }
            return (result != null && result is Boolean && (Boolean)result);
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
                return Stopped && InFlight ? "Enable Trigger" : Stopped ? "COntinue" : "Pause";
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
            if (Stopped || Status == SequenceEntityStatus.FINISHED) {
                return;
            }

            if (InFlight) return;

            if (Check() && Parent != null) {
                if (ItemUtility.IsInRootContainer(Parent) && this.Parent.Status == SequenceEntityStatus.RUNNING && this.Status != SequenceEntityStatus.DISABLED) {
                    Logger.Info("When Switch/Weather expression satisfied - Interrupting current Instruction Set");
                    sequenceNavigationVM.Sequence2VM.CancelSequenceCommand.Execute(this);
                    Status = SequenceEntityStatus.RUNNING;
                    cts = new CancellationTokenSource();
                    try {
                        // Wait a short time for the sequence to be canceled...
                        Thread.Sleep(1500);
                        Logger.Info("When Switch/Weather is starting user sequence.");
                        await Execute(new Progress<ApplicationStatus>(p => AppStatus = p), cts.Token);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    } finally {
                        //if (!Stopped) {
                        Logger.Info("When Switch/Weather finishing user sequence; restarting interrupted sequence.");
                        //Status = SequenceEntityStatus.CREATED;
                        Status = SequenceEntityStatus.FINISHED;
                        InFlight = false;
                        if (OnceOnly) Stopped = true;
                        sequenceNavigationVM.Sequence2VM.StartSequenceCommand.Execute(this);
                        //}
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

                if (!Check()) return;

                Logger.Info("When Switch/Weather: Expression satisfied.");

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
        public string ShowCurrentInfo() {
            return IfWhenSwitch.ShowCurrentInfo(Predicate, switchMediator, weatherMediator);
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
            RaisePropertyChanged("StartStop");
        }
    }
}