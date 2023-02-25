#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Wait Until False")]
    [ExportMetadata("Description", "Waits until the When Switch/Weather expression becomes false.")]
    [ExportMetadata("Icon", "SwitchesSVG")]
    [ExportMetadata("Category", "Switch")]
    [Export(typeof(ISequenceItem))]
    public class WaitUntilFalse : SequenceItem, IValidatable {

        [ImportingConstructor]
        public WaitUntilFalse() {
        }

        private WaitUntilFalse(WaitUntilFalse cloneMe) : this() {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new WaitUntilFalse(this);
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public TimeSpan WaitInterval { get; set; } = TimeSpan.FromSeconds(5);

        public bool Validate() {
            var i = new List<string>();

            if (!(Parent is IfContainer ifc && ifc.PseudoParent is WhenSwitch ws)) {
                // Walk up parents; sigh...
                i.Add("Wait Until False must be within a When Switch/Weather instruction.");
            }

            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitUntilFalse)}";
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Parent is IfContainer ifc && ifc.PseudoParent is WhenSwitch ws) {
                while (ws.Check()) {
                    progress?.Report(new ApplicationStatus() { Status = "Waiting for expression to be false" });
                    await CoreUtil.Wait(WaitInterval, token, default);
                }
            }
         }
    }
}