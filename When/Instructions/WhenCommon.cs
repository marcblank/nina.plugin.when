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
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Container;
using NINA.Core.Model;
using NINA.Sequencer.Conditions;
using Newtonsoft.Json;
using System.Reflection;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.ViewModel.Sequencer;
using NINA.WPF.Base.Interfaces.Mediator;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "When Becomes Unsafe")]
    [ExportMetadata("Description", "Runs a customizable set of instructions within seconds of an 'Unsafe' condition being recognized.")]
    [ExportMetadata("Icon", "ShieldSVG")]
    [JsonObject(MemberSerialization.OptIn)]

    public abstract class When : SequenceTrigger, IValidatable {
        protected ISafetyMonitorMediator safetyMediator;
        protected ISequenceMediator sequenceMediator;
        protected ISequenceNavigationVM sequenceNavigationVM;
        protected IApplicationStatusMediator applicationStatusMediator;
        protected ISwitchMediator switchMediator;
        protected IWeatherDataMediator weatherMediator;
        protected ICameraMediator cameraMediator;

        [ImportingConstructor]
        public When(ISafetyMonitorMediator safetyMediator, ISequenceMediator sequenceMediator, IApplicationStatusMediator applicationStatusMediator, ISwitchMediator switchMediator,
                IWeatherDataMediator weatherMediator, ICameraMediator cameraMediator) {
            this.safetyMediator = safetyMediator;
            this.sequenceMediator = sequenceMediator;
            this.applicationStatusMediator = applicationStatusMediator;
            this.switchMediator = switchMediator;
            this.weatherMediator = weatherMediator;
            this.cameraMediator = cameraMediator;
            ConditionWatchdog = new ConditionWatchdog(InterruptWhen, TimeSpan.FromSeconds(5));
            Instructions = new IfContainer();
            Instructions.AttachNewParent(Parent);
            Instructions.PseudoParent = this;
            Instructions.Name = Name;
            Instructions.Icon = Icon;
            var fields = sequenceMediator.GetType().GetRuntimeFields();
            foreach (FieldInfo fi in fields) {
                if (fi.Name.Equals("sequenceNavigation")) {

                    sequenceNavigationVM = (ISequenceNavigationVM)fi.GetValue(sequenceMediator);
                }
            }
        }

        public void Exex (object foo, EventArgs args) {
            Logger.Trace("Foo");

        }

        protected When(When cloneMe) : this(cloneMe.safetyMediator, cloneMe.sequenceMediator, cloneMe.applicationStatusMediator, cloneMe.switchMediator, cloneMe.weatherMediator, cloneMe.cameraMediator) {
            if (cloneMe != null) {
                CopyMetaData(cloneMe);
                Instructions = (IfContainer)cloneMe.Instructions.Clone();
                Instructions.AttachNewParent(Parent);
                Instructions.PseudoParent = this;
                Instructions.Name = cloneMe.Name;
                Instructions.Icon = cloneMe.Icon;
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

        private bool isSafe;

        public bool IsSafe {
            get => isSafe;
            protected set {
                isSafe = value;
                RaisePropertyChanged();
            }
        }

        private IList<string> issues = new List<string>();

        public override void Initialize() {
            base.Initialize();
            Instructions.Initialize();
        }

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        protected void CommonValidate() {
            if (Instructions.PseudoParent == null) {
                Instructions.PseudoParent = this;
            }

            // Avoid infinite loop by checking first...
            if (Instructions.Parent != Parent) {
                Instructions.AttachNewParent(Parent);
            }

            foreach (ISequenceItem item in Instructions.Items) {
                if (item is IValidatable val) {
                    //item.AttachNewParent(Parent);
                    _ = val.Validate();
                }
            }
        }

        public bool Validate() {
            CommonValidate();

            var i = new List<string>();

            foreach (ISequenceItem item in Instructions.Items) {
                if (item is IValidatable v) {
                    _ = v.Validate();
                }
            }

            Issues = i;
            return i.Count == 0;
        }

        public abstract bool Check();

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

        public string StartStop {
            get {
                return Stopped && InFlight ? "Reset Trigger" : Stopped ? "Restart" : "Pause";
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

        private ConditionWatchdog LoopWatchdog { get; set; }

        private bool Triggered { get; set; } = false;

        private bool Critical {  get; set; } = false;


        private async Task InterruptWhen() {
            Logger.Trace("*When Interrupt*");
            if (!sequenceMediator.IsAdvancedSequenceRunning()) return;
            if (InFlight || Triggered) {
                Logger.Trace("When: InFlight or Triggered, return");
                return;
            }

            if (ShouldTrigger(null, null) && Parent != null) {
                Logger.Info("When; shouldTrigger = true");
                if (ItemUtility.IsInRootContainer(Parent) && this.Parent.Status == SequenceEntityStatus.RUNNING && this.Status != SequenceEntityStatus.DISABLED) {
                    Triggered = true;
                    Logger.Info("When: Interrupting current Instruction Set");

                    Critical = true;
                    try {

                        sequenceMediator.CancelAdvancedSequence();
                        Logger.Info("When: Canceling sequence...");

                        await Task.Delay(1000);
                        while (sequenceMediator.IsAdvancedSequenceRunning()) {
                            Logger.Info("Delay 1000");
                            await Task.Delay(1000);
                        }
                        Logger.Info("When: Sequence longer running");
                    } finally {
                        Critical = false;
                    }

                    await sequenceMediator.StartAdvancedSequence(true);
                    Logger.Trace("When: Starting sequence, Triggered -> true");
                } else {
                    if (!ItemUtility.IsInRootContainer(Parent)) {
                        Logger.Trace("When: Parent not in root container?");
                    } else if (Parent.Status != SequenceEntityStatus.RUNNING) {
                        Logger.Trace("When: Parent is not running?");
                    } else {
                        Logger.Trace("WhenL Disabled?");
                    }
                }
            } else {
                Logger.Trace("When: Should trigger = false");
            }
        }

        public override string ToString() {
            return $"Trigger: {nameof(When)} - toString";
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (InFlight) {
                Logger.Trace("ShouldTrigger: FALSE (InFlight) ");
                return false;
            }
            if (!Check()) {
                Logger.Info("ShouldTrigger: TRUE, TriggerRunner set");
                TriggerRunner = Instructions;
                return true;
            }
            Logger.Trace("ShouldTrigger: FALSE");
            return false;
        }

        public async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Info("Execute");
            if (Critical) {
                Logger.Info("When: Execute in critical section; return");
                return;
            }
            if (InFlight) {
                Logger.Info("When: InFlight; return");
                return; 
            }
            try {
                Logger.Info("When: running TriggerRunner, InFlight -> true, Triggered -> false");
                InFlight = true;
                Triggered = false;
                await TriggerRunner.Run(progress, token);
            } finally {
                InFlight = false;
                Triggered = false;
                if (this is WhenSwitch w && w.OnceOnly) {
                    w.Disabled = true;
                }
                Logger.Info("When: Execute done; InFlight -> false, Triggered false");
            }
        }

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            return Execute(progress, token);
        }
    }
}