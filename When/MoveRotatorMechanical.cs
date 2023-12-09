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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Sequencer.SequenceItem;
using NINA.Profile;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Rotate by Mechanical Angle +")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Rotator_MoveRotatorMechanical_Description")]
    [ExportMetadata("Icon", "RotatorSVG")]
    [ExportMetadata("Category", "Powerups (Enhanced Instructions)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class MoveRotatorMechanical : SequenceItem, IValidatable {

        [ImportingConstructor]
        public MoveRotatorMechanical(IRotatorMediator RotatorMediator) {
            this.rotatorMediator = RotatorMediator;
        }

        private MoveRotatorMechanical(MoveRotatorMechanical cloneMe) : this(cloneMe.rotatorMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new MoveRotatorMechanical(this) {
                MechanicalPosition = MechanicalPosition
            };
        }

        private IRotatorMediator rotatorMediator;

        private double mechanicalPosition = 0;

        [JsonProperty]
        public double MechanicalPosition {
            get => mechanicalPosition;
            set {
                mechanicalPosition = value;
                RaisePropertyChanged();
            }
        }

        private string mechanicalPositionExpr = "";

        [JsonProperty]
        public string MechanicalPositionExpr {
            get => mechanicalPositionExpr;
            set {
                mechanicalPositionExpr = value;
                ConstantExpression.Evaluate(this, "MechanicalPositionExpr", "MechanicalPosition", 0);
                RaisePropertyChanged("MechanicalPositionExpr");
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
            return rotatorMediator.MoveMechanical((float)MechanicalPosition, token);
        }

        public bool Validate() {
            var i = new List<string>();
            if (!rotatorMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblRotatorNotConnected"]);
            }
            ConstantExpression.Evaluate(this, "MechanicalPositionExpr", "MechanicalPosition", 0, i);

            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(MoveRotatorMechanical)}, Mechanical Position: {MechanicalPosition}";
        }
    }
}