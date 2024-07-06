using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Profile;
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

        private bool isOpen = false;
        public bool IsOpen {
            get { return isOpen; }
            set {
                isOpen = value;
                RaisePropertyChanged(nameof(IsOpen));
            }
        }

        private string displayType = "Numeric";
        public string DisplayType {
            get => displayType;
            set {
                displayType = value;
                RaisePropertyChanged("DockableValue");
                WhenPluginDockable.SaveDockableExprs();
            }
        }

        public string DockableValue {
            get {
                Evaluate();
                if (Error != null) {
                    return Error;
                }
                if (DisplayType.Equals("Numeric")) {
                    return Math.Round(Value, 2).ToString();
                } else if (DisplayType.Equals("Boolean")) {
                    return (Value == 0) ? "False" : "True";
                } else {
                    FilterWheelInfo fwi = WhenPlugin.FilterWheelMediator.GetInfo();
                    if (fwi == null || fwi.Connected == false) {
                        return "Not connected";
                    }
                    var filters = WhenPlugin.ProfileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                    if (Value < filters.Count) {
                        return filters[(int)Value].Name;
                    }
                    return "No filter";
                }
            }
        }



    }
}
