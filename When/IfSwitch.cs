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
using NINA.Core.Enum;
using System.Linq;
using System.Text;
using Accord.IO;
using Namotion.Reflection;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Interfaces;
using NINA.Sequencer.SequenceItem.Utility;
using System.Windows;
using NINA.Equipment.Equipment.MyWeatherData;
using System.Windows.Controls;
using System.Diagnostics;
using NINA.Core.Utility;

namespace WhenPlugin.When {
    [ExportMetadata("Name", "If Switch/Weather")]
    [ExportMetadata("Description", "Executes an instruction set if the expression, based on current switch and/or weather values, is true")]
    [ExportMetadata("Icon", "SwitchesSVG")]
    [ExportMetadata("Category", "Switch")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    
    public class IfSwitch : IfCommand, IValidatable {
        private ISwitchMediator switchMediator;
        private IWeatherDataMediator weatherMediator;

        [ImportingConstructor]
        public IfSwitch(ISwitchMediator switchMediator, IWeatherDataMediator weatherMediator) {
            Predicate = "";
            Instructions = new IfContainer();
            Instructions.PseudoParent = this;
            this.switchMediator = switchMediator;
            this.weatherMediator = weatherMediator;
        }

        public IfSwitch(IfSwitch copyMe) : this(copyMe.switchMediator, copyMe.weatherMediator) {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Predicate = copyMe.Predicate;
                Instructions = (IfContainer)copyMe.Instructions.Clone();
                Instructions.PseudoParent = this;
            }
        }

        public override object Clone() {
            return new IfSwitch(this) {
            };
        }

        public object EvaluatePredicate() {
            NCalc.Expression e = new NCalc.Expression(Predicate);

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
                foreach (string dataName in weatherData) {
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

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
 
            if (Predicate.IsNullOrEmpty()) {
                Status = SequenceEntityStatus.FAILED;
                return;
            }

            while (true) {
                try {
                    object result = EvaluatePredicate();
                    if (result == null) {
                        // Syntax error...
                        Logger.Info("IfSwitch: There is a syntax error in your predicate expression.");
                        Status = NINA.Core.Enum.SequenceEntityStatus.FAILED;
                        return;
                    }

                    if (result != null && result is Boolean && (Boolean)result) {
                        Logger.Info("IfSwitch: If Predicate is true!");
                        Runner runner = new Runner(Instructions, null, progress, token);
                        await runner.RunConditional();
                        if (runner.ShouldRetry) {
                            runner.ResetProgress();
                            runner.ShouldRetry = false;
                            Logger.Info("IfSwitch; retrying the failed instruction");
                            continue;
                        }
                    } else {
                        return;
                    }
                } catch (ArgumentException ex) {
                    Logger.Info("IfSwitch error: " + ex.Message);
                    Status = SequenceEntityStatus.FAILED;
                }
                return;
            }
        }

        [JsonProperty]
        public string Predicate { get; set; }

        [JsonProperty]
        public string PredicateValue { get; set; }  

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(IfSwitch)}, Predicate: {Predicate}";
        }

        public new bool Validate() {
            var i = new List<string>();

            if (Instructions.PseudoParent == null) {
                Instructions.PseudoParent = this;
            }

            if (Predicate.IsNullOrEmpty()) {
                i.Add("Expression cannot be empty!");
            }

            try {
                EvaluatePredicate();
            } catch (Exception ex) {
                i.Add("Error in expression: " + ex.Message);
            }

            PredicateValue = "";

            if (!switchMediator.GetInfo().Connected) {
                i.Add("Switch not connected");
                Issues = i;
                return false;
            }

            SetSwitchList();

            Issues = i;
            return i.Count == 0;
        }
 

        private IList<string> switches = new List<string>();
       
        public IList<string> Switches {
            get => switches;
            set {
                switches = value;
                RaisePropertyChanged();
            }
        }

        public string ShowCurrentInfo() {
            try {
                object result = EvaluatePredicate();
                if (result == null) {
                    return "There is a syntax error in the expression.";
                } else {
                    return "Your expression is currently: " + result.ToString();
                }
            } catch (Exception ex) {
                return "Error: " + ex.Message;
            }
        }

        private string[] weatherData = new string[] { "CloudCover", "DewPoint", "Humidity", "Pressure", "RainRate", "SkyBrightness", "SkyQuality", "SkyTemperature", "StarFWHM", "Temperature",
            "WindDirection", "WindGust", "WindSpeed"};

        public void SetSwitchList() {
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
                foreach (string dataName in weatherData) {
                    double value = 0;
                    double t = weatherInfo.TryGetPropertyValue(dataName, value);
                    if (!Double.IsNaN(t)) {
                        i.Add("Weather: " + RemoveSpecialCharacters(dataName) + " (" + t + ")");
                    }
                }
            }

            foreach (ISequenceItem item in Instructions.Items) {
                if (item is IValidatable val) {
                    _ = val.Validate();
                }
            }

            Switches = i;
            RaisePropertyChanged("Switches");
        }
    }
}
