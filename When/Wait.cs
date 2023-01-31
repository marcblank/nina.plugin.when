#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Math.Comparers;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Wait")]
    [ExportMetadata("Description", "Wait indefinitely (until instruction is skipped or deleted)")]
    [ExportMetadata("Icon", "HourglassSVG")]
    [ExportMetadata("Category", "When")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class Wait : SequenceItem {

        [ImportingConstructor]
        public Wait() {
            Time = 60*60*12;  // 12 hours
        }

        private Wait(Wait cloneMe) : base(cloneMe) {
        }

        public override object Clone() {
            return new Wait(this) {
                Time = Time
            };
        }

        private int time;

        [JsonProperty]
        public int Time {
            get => time;
            set {
                time = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return NINA.Core.Utility.CoreUtil.Wait(GetEstimatedDuration(), true, token, progress, "");
        }

        public override TimeSpan GetEstimatedDuration() {
            return TimeSpan.FromSeconds(Time);
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(Wait)}, Time: 12 hours";
        }
    }
}