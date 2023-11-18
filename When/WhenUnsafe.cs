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

namespace WhenPlugin.When {

    [ExportMetadata("Name", "When Becomes Unsafe")]
    [ExportMetadata("Description", "Runs a customizable set of instructions within seconds of an 'Unsafe' condition being recognized.")]
    [ExportMetadata("Icon", "ShieldSVG")]
    [ExportMetadata("Category", "Powerups (Safety)")]
    [Export(typeof(ISequenceTrigger))]

    public class WhenUnsafe : When { 
 
        [ImportingConstructor]
        public WhenUnsafe (ISafetyMonitorMediator safetyMediator, ISequenceMediator sequenceMediator, IApplicationStatusMediator applicationStatusMediator, ISwitchMediator switchMediator,
                IWeatherDataMediator weatherMediator) 
            : base(safetyMediator, sequenceMediator, applicationStatusMediator, switchMediator, weatherMediator) {
        }

        protected WhenUnsafe(WhenUnsafe cloneMe) : base(cloneMe.safetyMediator, cloneMe.sequenceMediator, cloneMe.applicationStatusMediator, cloneMe.switchMediator, cloneMe.weatherMediator) {
            if (cloneMe != null) {
                CopyMetaData(cloneMe);
                Instructions = (IfContainer)cloneMe.Instructions.Clone();
                Instructions.AttachNewParent(Parent);
                Instructions.PseudoParent = this;
                Instructions.Name = Name;
                Instructions.Icon = Icon;
            }
        }

        public override object Clone() {
            return new WhenUnsafe(this);
        }

        protected bool IsActive() {
            return ItemUtility.IsInRootContainer(Parent) && Parent.Status == SequenceEntityStatus.RUNNING && Status != SequenceEntityStatus.DISABLED;
        }

        public static bool CheckSafe(ISequenceEntity item, ISafetyMonitorMediator safetyMediator) {
            var info = safetyMediator.GetInfo();
            bool safe = info.Connected && info.IsSafe;

            // SAFE means IsSafe && SAFE != false
            double safeValue = Double.NaN;
            bool valid = ConstantExpression.IsValidConverter(item, "SAFE", out safeValue, null);

            if (safe && valid && safeValue == 0) {
                safe = false;
            }
            return safe;
        }

        public override bool Check() {
            bool IsSafe = CheckSafe(this, safetyMediator);

            //if (!IsSafe && IsActive()) {
            //    Logger.Info($"{nameof(SafetyMonitorCondition)} finished. Status=Unsafe");
            //}
            return IsSafe && IsActive();
        }
    }
}