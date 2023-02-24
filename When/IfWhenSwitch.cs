using Namotion.Reflection;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Mediator;
using NINA.WPF.Base.ViewModel.Equipment.WeatherData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhenPlugin.When {
    public class IfWhenSwitch {


        public static bool HasSpecialChars(string str) {
            return str.Any(ch => !char.IsLetterOrDigit(ch));
        }

        public static string RemoveSpecialCharacters(string str) {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str) {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_') {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string[] WeatherData = new string[] { "CloudCover", "DewPoint", "Humidity", "Pressure", "RainRate", "SkyBrightness", "SkyQuality", "SkyTemperature",
            "StarFWHM", "Temperature", "WindDirection", "WindGust", "WindSpeed"};

        public static object EvaluatePredicate(string predicate, ISwitchMediator switchMediator, IWeatherDataMediator weatherMediator) {
            NCalc.Expression e = new NCalc.Expression(predicate);

            // Get switch values
            SwitchInfo switchInfo = switchMediator.GetInfo();
            if (switchInfo.Connected) {
                foreach (ISwitch sw in switchInfo.ReadonlySwitches) {
                    string key = RemoveSpecialCharacters(sw.Name);
                    e.Parameters[key] = sw.Value;
                }
                foreach (ISwitch sw in switchInfo.WritableSwitches) {
                    string key = RemoveSpecialCharacters(sw.Name);
                    e.Parameters[key] = sw.Value;
                }
            }

            // Get weather values
            WeatherDataInfo weatherInfo = weatherMediator.GetInfo();
            if (weatherInfo.Connected) {
                foreach (string dataName in WeatherData) {
                    double value = 0;
                    double t = weatherInfo.TryGetPropertyValue(dataName, value);
                    if (!Double.IsNaN(t)) {
                        e.Parameters[RemoveSpecialCharacters(dataName)] = t;
                    }
                }
            }

            // Evaluate predicate
            if (e.HasErrors()) {
                // Syntax error...
                throw new Exception("The expression has a syntax error.");
            }

            return e.Evaluate();
        }

        public static List<string> GetSwitchList(ISwitchMediator switchMediator, IWeatherDataMediator weatherMediator) {
            var i = new List<string>();
            SwitchInfo switchInfo = switchMediator.GetInfo();
            WeatherDataInfo weatherInfo = weatherMediator.GetInfo();

            if (switchInfo.Connected) {
                foreach (ISwitch sw in switchInfo.ReadonlySwitches) {
                    i.Add("Gauge: " + RemoveSpecialCharacters(sw.Name) + " (" + sw.Value + ")");
                }
                foreach (ISwitch sw in switchInfo.WritableSwitches) {
                    i.Add("Switch: " + RemoveSpecialCharacters(sw.Name) + " (" + sw.Value + ")");
                }
            }

            if (weatherInfo.Connected) {
                foreach (string dataName in WeatherData) {
                    double value = 0;
                    double t = weatherInfo.TryGetPropertyValue(dataName, value);
                    if (!Double.IsNaN(t)) {
                        i.Add("Weather: " + RemoveSpecialCharacters(dataName) + " (" + t + ")");
                    }
                }
            }

            return i;
        }

        public static string ShowCurrentInfo(string predicate, ISwitchMediator switchMediator, IWeatherDataMediator weatherMediator) {
            try {
                object result = EvaluatePredicate(predicate, switchMediator, weatherMediator);
                if (result == null) {
                    return "There is a syntax error in the expression.";
                } else {
                    return "Your expression is currently: " + result.ToString();
                }
            } catch (Exception ex) {
                return "Error: " + ex.Message;
            }
        }


    }
}
