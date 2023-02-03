using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using NCalc;
using Castle.Core.Internal;
using NINA.Core.Utility.Notification;
using System.Linq;
using System.Text;
using Accord.IO;
using Namotion.Reflection;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Trigger;
using System.Windows.Input;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Equipment.MySafetyMonitor;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "If Unsafe")]
    [ExportMetadata("Description", "Sequencer Powerups:\r\nExecutes a customizable instruction set if the safety monitor indicates that conditions are unsafe.")]
    [ExportMetadata("Icon", "ShieldSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_SafetyMonitor")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class IfUnsafe : IfSafeUnsafe, IValidatable {

 
        [ImportingConstructor]
        public IfUnsafe(ISafetyMonitorMediator safetyMediator) : base(safetyMediator, false) {
            Name = Name;
        }

        public IfUnsafe(IfUnsafe copyMe) : this(copyMe.safetyMediator) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Instructions = (IfContainer)copyMe.Instructions.Clone();
            }
        }

        public override object Clone() {
            return new IfUnsafe(this) {
            };
        }
    }
}
