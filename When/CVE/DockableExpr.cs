using NINA.Sequencer.Container;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhenPlugin.When {
    public class DockableExpr : Expr {

        public DockableExpr (string expression) : base(new SetVariable(), expression) {
            SequenceEntity.AttachNewParent(PseudoRoot);
        }

        public static SequenceRootContainer PseudoRoot = new SequenceRootContainer();

        public override string Expression {
            get {
                return base.Expression;
            }
            set {
                base.Expression = value;
                // Note it's changed; save the list, remove empty items...
                if (value != null && value.Length == 0) {
                    // Remove it...
                    WhenPluginDockable.RemoveExpr(this);
                }
                WhenPluginDockable.SaveDockableExprs();
            }
        }
    }
}
