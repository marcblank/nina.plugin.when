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

        private string conversionType = "None";
        public string ConversionType {
            get => conversionType;
            set {
                conversionType = value;
                RaisePropertyChanged("DockableValue");
                WhenPluginDockable.SaveDockableExprs();
            }
        }


        private const long ONE_YEAR = 60 * 60 * 24 * 365;
        public static string ExprValueString(double value) {
            long start = DateTimeOffset.Now.ToUnixTimeSeconds() - ONE_YEAR;
            long end = start + (2 * ONE_YEAR);
            if (value > start && value < end) {
                DateTime dt = ConvertFromUnixTimestamp(value).ToLocalTime();
                if (dt.Day == DateTime.Now.Day + 1) {
                    return dt.ToShortTimeString() + " tomorrow";
                } else if (dt.Day == DateTime.Now.Day - 1) {
                    return dt.ToShortTimeString() + " yesterday";
                } else
                    return dt.ToShortTimeString();
            } else {
                return value.ToString();
            }
        }

        public string DockableValue {
            get {
                Evaluate();
                if (Error != null) {
                    return Error;
                }
                if (DisplayType.Equals("Numeric")) {

                    if (ConversionType.Equals("C to F")) {
                        return Math.Round(32 + (Value * 9 / 5), 2).ToString() + "° F";
                    } else if (ConversionType.Equals("m/s to mph")) {
                        return Math.Round(Value * 2.237, 2).ToString() + " mph";
                    } else if (ConversionType.Equals("kph to mph")) {
                        return Math.Round(Value * .621, 2).ToString() + " mph";
                    } else if (ConversionType.Equals("hPa to inhg")) {
                        return Math.Round(Value * .0295, 2).ToString() + "\" hg";
                    }

                    return ExprValueString(Math.Round(Value, 2)); ;
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
