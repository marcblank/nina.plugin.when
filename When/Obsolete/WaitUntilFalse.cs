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
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Wait Until False (Deprecated)")]
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

        private IIfWhenSwitch FindIfWhen() {
            SequenceContainer p = (SequenceContainer)Parent;
            while (p != null) {
                foreach (SequenceTrigger t in p.Triggers) {
                    if (t is IIfWhenSwitch ws) {
                        return ws;
                    }
                }
                foreach (SequenceItem item in p.Items) {
                    if (item is IIfWhenSwitch ws) {
                        return ws;
                    }
                }
                if (p is IfContainer ifc) {
                    p = (SequenceContainer)ifc.PseudoParent?.Parent;
                } else {
                    p = (SequenceContainer)p.Parent;
                }
            }
            return null;
        }
        public bool Validate() {
            var i = new List<string>();

            IIfWhenSwitch ws = FindIfWhen();
            if (ws == null) {
                // Walk up parents; sigh...
                i.Add("Wait Until False must be within an If or When Switch/Weather instruction.");
            }

            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitUntilFalse)}";
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            IIfWhenSwitch ws = FindIfWhen();
            if (ws != null) {
                while (ws.Check()) {
                    progress?.Report(new ApplicationStatus() { Status = "Waiting for expression to be false" });
                    await CoreUtil.Wait(WaitInterval, token, default);
                }
            }
         }
    }
}