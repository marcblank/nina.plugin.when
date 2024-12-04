﻿#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Interfaces.Mediator;
using System;
using System.ComponentModel.Composition;
using NINA.Core.Enum;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Sequencer;
using NINA.Core.Utility;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "When Becomes Unsafe")]
    [ExportMetadata("Description", "Runs a customizable set of instructions within seconds of an 'Unsafe' condition being recognized.")]
    [ExportMetadata("Icon", "ShieldSVG")]
    [ExportMetadata("Category", "Powerups (Safety)")]
    [Export(typeof(ISequenceTrigger))]

    public class WhenUnsafe : When { 
 
        [ImportingConstructor]
        public WhenUnsafe (ISafetyMonitorMediator safetyMediator, ISequenceMediator sequenceMediator, IApplicationStatusMediator applicationStatusMediator, ISwitchMediator switchMediator,
                IWeatherDataMediator weatherMediator, ICameraMediator cameraMediator) 
            : base(safetyMediator, sequenceMediator, applicationStatusMediator, switchMediator, weatherMediator, cameraMediator) {
        }

        protected WhenUnsafe(WhenUnsafe cloneMe) : base(cloneMe.safetyMediator, cloneMe.sequenceMediator, cloneMe.applicationStatusMediator, cloneMe.switchMediator, cloneMe.weatherMediator, cloneMe.cameraMediator) {
            if (cloneMe != null) {
                CopyMetaData(cloneMe);
                Instructions = (IfContainer)cloneMe.Instructions.Clone();
                Instructions.AttachNewParent(Parent);
                Instructions.PseudoParent = this;
                Instructions.Name = cloneMe.Name;
                Instructions.Icon = cloneMe.Icon;
            }
        }

        public override object Clone() {
            return new WhenUnsafe(this);
        }

        protected bool IsActive() {
            return ItemUtility.IsInRootContainer(Parent) && Parent.Status == SequenceEntityStatus.RUNNING && Status != SequenceEntityStatus.DISABLED;
        }

        public override string ToString() {
            return $"Trigger: {nameof(WhenUnsafe)}";
        }


        public static bool CheckSafe(ISequenceEntity item, ISafetyMonitorMediator safetyMediator) {
            var info = safetyMediator.GetInfo();
            bool safe = info.Connected && info.IsSafe;

            if (!safe) {
                Logger.Info("IsSafe is FALSE; connected = " + info.Connected + ", IsSafe = " + info.IsSafe);
            }

            
            double safeValue = Double.NaN;
            Symbol sym = Symbol.FindSymbol("SAFE", item.Parent);
            if (sym != null) {
                // If "SAFE" is defined, safe is the value
                Logger.Info("SAFE is defined with value: " + sym.Expr.Value);
                safeValue = sym.Expr.Value;
                return (safeValue != 0);
            }
            // Otherwise, it's the safety monitor value
            return safe;
        }

        public override bool Check() {
            bool IsSafe = CheckSafe(this, safetyMediator);
            //Logger.Info("When Unsafe: Check returning " + ((IsSafe && IsActive()) ? "TRUE" : "FALSE"));
            return IsSafe; // && IsActive();
        }
    }
}