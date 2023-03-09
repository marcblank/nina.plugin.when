using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Equipment.MySafetyMonitor;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "If Safe")]
    [ExportMetadata("Description", "Executes a customizable instruction set if the safety monitor indicates that conditions are safe.")]
    [ExportMetadata("Icon", "ShieldSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_SafetyMonitor")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class IfSafe : IfSafeUnsafe, IValidatable {

        [ImportingConstructor]
        public IfSafe(ISafetyMonitorMediator safetyMediator) : base(safetyMediator, true) {
        }

        public IfSafe(IfSafe copyMe) : base(copyMe) {
        }

        public override object Clone() {
            return new IfSafe(this) {
            };
        }
        
    }
}
