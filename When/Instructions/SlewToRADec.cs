#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.Validations;
using NINA.Astrometry;
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
using NINA.Core.Utility.Notification;
using NINA.Sequencer.SequenceItem;

namespace WhenPlugin.When {

    [ExportMetadata("Name", "Slew to RA/Dec")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Telescope_SlewScopeToRaDec_Description")]
    [ExportMetadata("Icon", "SlewToRaDecSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    //[Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SlewToRADec : SequenceItem, IValidatable {

        [ImportingConstructor]
        public SlewToRADec(ITelescopeMediator telescopeMediator, IGuiderMediator guiderMediator) {
            this.telescopeMediator = telescopeMediator;
            this.guiderMediator = guiderMediator;
            Coordinates = new InputCoordinates();
            RAExpr = new Expr(this);
            DecExpr = new Expr(this);
        }

        private SlewToRADec(SlewToRADec cloneMe) : this(cloneMe.telescopeMediator, cloneMe.guiderMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            SlewToRADec clone = new SlewToRADec(this) {
                Coordinates = Coordinates?.Clone()
            };
            clone.RAExpr = new Expr(clone, this.RAExpr.Expression);
            clone.DecExpr = new Expr(clone, this.DecExpr.Expression);
            return clone;
        }

        private ITelescopeMediator telescopeMediator;
        private IGuiderMediator guiderMediator;

        // 0 to 24
        private Expr _RAExpr = null;

        [JsonProperty]
        public Expr RAExpr {
            get => _RAExpr;
            set {
                _RAExpr = value;
                RaisePropertyChanged();
            }
        }
        
        // -90 to 90
        private Expr _DecExpr = null;

        [JsonProperty]
        public Expr DecExpr {
            get => _DecExpr;
            set {
                _DecExpr = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public InputCoordinates Coordinates { get; set; }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (telescopeMediator.GetInfo().AtPark) {
                Notification.ShowError(Loc.Instance["LblTelescopeParkedWarning"]);
                throw new SequenceEntityFailedException(Loc.Instance["LblTelescopeParkedWarning"]);
            }

            var stoppedGuiding = await guiderMediator.StopGuiding(token);
            await telescopeMediator.SlewToCoordinatesAsync(Coordinates.Coordinates, token);
            if (stoppedGuiding) {
                await guiderMediator.StartGuiding(false, progress, token);
            }
        }

        public override void AfterParentChanged() {
            Validate();
        }
        public bool Validate() {
            var i = new List<string>();
            var info = telescopeMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Loc.Instance["LblTelescopeNotConnected"]);
            }

            RAExpr.Validate();
            DecExpr.Validate();

            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SlewToRADec)}, Coordinates: {Coordinates}";
        }
    }
}