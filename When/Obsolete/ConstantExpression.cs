using Namotion.Reflection;
using NCalc;
using NCalc.Domain;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using NINA.Sequencer.Conditions;
using System.Threading.Tasks;
using NINA.Equipment.Equipment.MyCamera;
using System.Threading;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySafetyMonitor;

namespace WhenPlugin.When {
    public class ConstantExpression {
        public class Keys : Dictionary<string, object> {

            public override string ToString() {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, object> kvp in this) {
                    sb.Append(kvp.Key);
                    sb.Append(' ');
                }
                return sb.ToString();
            }
        }

        static public SequenceContainer GlobalContainer = new SequentialContainer() { Name = "Global Constants" };

        public static bool IsValid(object obj, string exprName, string expr, out double val, IList<string> issues) {
            val = 0;
            return false;
        }

        public static bool Evaluate(ISequenceEntity item, string exprName, string valueName, object def) {
            return Evaluate(item, exprName, valueName, def, null);
        }


        public static bool Evaluate(ISequenceEntity item, string exprName, string valueName, object def, IList<string> issues) {
            return false;
        }
    }
}
