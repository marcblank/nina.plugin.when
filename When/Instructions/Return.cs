#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Validations;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Sequencer.SequenceItem;
using CsvHelper;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Return")]
    [ExportMetadata("Description", "Return a value from a Function")]
    [ExportMetadata("Icon", "MoveFocuserRelativeSVG")]
    [ExportMetadata("Category", "Powerups (Fun-ctions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class Return : SequenceItem, IValidatable {

        [ImportingConstructor]
        public Return(IFocuserMediator focuserMediator) {
            this.focuserMediator = focuserMediator;
            RExpr = new Expr(this);
        }

        private Return(Return cloneMe) : this(cloneMe.focuserMediator) {
            CopyMetaData(cloneMe);
            RExpr = new Expr(this, cloneMe.RExpr.Expression, "Integer");
            RExpr.Default = 0;
        }

        public override object Clone() {
            return new Return(this) {
            };
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Validate();
        }

        [JsonProperty]
        public Expr RExpr { get; set; }


        private IFocuserMediator focuserMediator;


        [JsonProperty]
        public string RelativePositionExpr {
            get => null;
            set {
                RExpr.Expression = value;
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

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            //return focuserMediator.MoveFocuserRelative((int)RExpr.Value, token);
            return Task.CompletedTask;
        }

        public bool Validate() {
            var i = new List<string>();
            if (!focuserMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblFocuserNotConnected"]);
            }
            RExpr.Validate();
            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(Return)}, Relative Position: {RExpr.Value}";
        }
    }
}