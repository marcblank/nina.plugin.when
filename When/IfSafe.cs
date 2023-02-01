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
    [ExportMetadata("Description", "Executes an instruction set if the safety monitor indicates that conditions are safe.")]
    [ExportMetadata("Icon", "ShieldSVG")]
    [ExportMetadata("Category", "When (and If)")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class IfSafe : IfSafeUnsafe, IValidatable {


        [ImportingConstructor]
        public IfSafe(ISafetyMonitorMediator safetyMediator) : base(safetyMediator, true) {
            Name = Name;
        }

        public IfSafe(IfSafe copyMe) : this(copyMe.safetyMediator) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Instructions = (IfContainer)copyMe.Instructions.Clone();
            }
        }

        public override object Clone() {
            return new IfSafe(this) {
            };
        }
    }
}
