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
using NINA.Sequencer;
using System.Windows.Media.Converters;
using NINA.WPF.Base.Mediator;
using Accord.IO;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "When Becomes Unsafe")]
    [ExportMetadata("Description", "Runs a customizable set of instructions within seconds of an 'Unsafe' condition being recognized.")]
    [ExportMetadata("Icon", "ShieldSVG")]
    //[ExportMetadata("Category", "Lbl_SequenceCategory_SafetyMonitor")]
    //[Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]

    public abstract class When : SequenceTrigger, IValidatable, IIfWhenSwitch {
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
            ConditionWatchdog = new ConditionWatchdog(InterruptWhenUnsafe, TimeSpan.FromSeconds(5));
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
            Logger.Info("Foo");

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

        private CancellationTokenSource cts;


        private string startStop = "Stop";
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

        private async Task InterruptWhenUnsafe() {
            if (InFlight || Triggered) return;

            if (ShouldTrigger(null, null) && Parent != null) {
                if (ItemUtility.IsInRootContainer(Parent) && this.Parent.Status == SequenceEntityStatus.RUNNING && this.Status != SequenceEntityStatus.DISABLED) {
                    Triggered = true;
                    Logger.Info("Unsafe conditions detected - Interrupting current Instruction Set");

                    var root = ItemUtility.GetRootContainer(Parent);
                    await root?.Interrupt();
                    //sequenceNavigationVM.Sequence2VM.CancelSequenceCommand.Execute(true);
                    await Task.Delay(2000);

                    while (!cameraMediator.IsFreeToCapture(this)) {
                        Logger.Error("Wait 1");
                        await Task.Delay(1000);
                    };

                    _ = sequenceNavigationVM.Sequence2VM.StartSequenceCommand.ExecuteAsync(true);

                    //await Task.Delay(1000);
                    //sequenceNavigationVM.Sequence2VM.StartSequenceCommand.Execute(true);
                    //await Task.Delay(1000);
                }
            }
        }

        public override string ToString() {
            return $"Condition: {nameof(When)}";
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (InFlight) return false;
            if (!Check()) {
                TriggerRunner = Instructions;
                return true;
            }
            return false;
        }

        public async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (InFlight) return;
            try {
                InFlight = true;
                Triggered = false;
                await TriggerRunner.Run(progress, token);
            } finally {
                InFlight = false;
                Triggered = false;
                InterruptWhenUnsafe();
            }
        }

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            return Execute(progress, token);
        }
    }
}