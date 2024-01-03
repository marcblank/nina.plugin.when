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
        static SequenceRootContainer FindRoot(ISequenceEntity cont) {
            while (cont != null) {
                if (cont is SequenceRootContainer root) { return root; }
                if (cont is IfContainer ifc) {
                    cont = ifc.PseudoParent;
                } else {
                    cont = cont.Parent;
                }
            }
            return null;
        }

        static public Dictionary<ISequenceEntity, Keys> KeyCache = new Dictionary<ISequenceEntity, Keys>();

        static private bool Debugging = false;

        static private Semaphore SEM = new Semaphore(initialCount: 1, maximumCount: 1);

        public class Keys : Dictionary<string, object> {

            public Keys Clone() {
                Keys k = new Keys();
                foreach (KeyValuePair<string, object> kv in this) {
                    k.Add(kv.Key, kv.Value);
                }
                return k;
            }

            public override string ToString() {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, object> kvp in this) {
                    sb.Append(kvp.Key);
                    sb.Append(' ');
                }
                return sb.ToString();
            }
        }

        public class Var {
            SetVariable svInstruction;
            public Var(SetVariable item) {
                svInstruction = item;
            }

            public SetVariable GetSetVariableInstruction() { return svInstruction; }
        }

        private static Stack<Keys> KeysStack = new Stack<Keys>();

        private static int FC = 0;

        static public void FlushKeys() {
            //KeyCache.Clear();
        }

        static public void FlushContainerKeys(ISequenceContainer container) {
            lock (ConstantsLock) {
                KeyCache.Remove(container, out _);
            }
        }

        static public ISequenceContainer GetRoot(ISequenceEntity item) {
            if (item == null) return null;
            ISequenceContainer p = item.Parent;
            while (p != null) {
                if (p is ISequenceRootContainer root) {
                    return root;
                }
                p = p.Parent;
            }
            return null;
        }

        private static readonly object ConstantsLock = new object();

        static public void UpdateConstants(ISequenceEntity item) {
        }

        static public SequenceContainer GlobalContainer = new SequentialContainer() { Name = "Global Constants" };

        static private bool InFlight { get; set; } = false;

        static private int FCDepth = 0;

        static private void FindConstantsRoot(ISequenceContainer container, Keys keys) {
        }

        static private Double NCalcEvaluate(string expr, Keys mergedKeys, IList<string> issues) {
            return 0;
        }

        static private Double EvaluateExpression(ISequenceEntity item, string expr, Stack<Keys> stack, IList<string> issues) {
            return 0;
        }

        static Keys GetMergedKeys(Stack<Keys> stack) {
            return new Keys();
        }

        static public Keys GetParsedKeys(LogicalExpression e, Keys mergedKeys, Keys k) {
            return new Keys();
        }

        static public string FindKey(ISequenceEntity item, string key) {
            return "Nowhere";
        }

        static public ISequenceEntity FindKeyContainer(ISequenceEntity item, string key) {
            return null;
        }

        static public string DissectExpression(ISequenceEntity item, string expr, Stack<Keys> stack) {
            return null;
        }
 
        static public Stack<Keys> GetKeyStack(ISequenceEntity item) {
            return new Stack<Keys>();
        }


        static ISequenceEntity GetParent(ISequenceEntity p) {
            if (p is IfContainer ic) {
                return (ic.Parent == null ? ic.PseudoParent : ic.Parent);
            } else {
                return p.Parent;
            }

        }

        static private bool IsAttachedToRoot(ISequenceContainer container) {
            ISequenceEntity p = container;
            while (p != null) {
                if (p is SequenceRootContainer) {
                    return true;
                } else {
                    p = GetParent(p);
                }
            }
            return false;
        }

        static private void FindConstants(ISequenceContainer container, Keys keys) {
        }

        private static bool Loaded { get; set; } = false;

        public static bool IsValid(object obj, string exprName, string expr, out double val, IList<string> issues) {
            val = 0;
            return false;
        }

        public static bool Evaluate(ISequenceEntity item, string exprName, string valueName, object def) {
            lock (ConstantsLock) {
                return Evaluate(item, exprName, valueName, def, null);
            }
        }

        
        public static int EvaluateCount = 0;


        public static bool Evaluate(ISequenceEntity item, string exprName, string valueName, object def, IList<string> issues) {
            return false;
        }

        private static void DebugInfo(params string[] strs) {
            if (Debugging) {
                Logger.Debug(String.Join("", strs));
            }
        }

        private static void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            throw new NotImplementedException();
        }

        private static string[] WeatherData = new string[] { "CloudCover", "DewPoint", "Humidity", "Pressure", "RainRate", "SkyBrightness", "SkyQuality", "SkyTemperature",
            "StarFWHM", "Temperature", "WindDirection", "WindGust", "WindSpeed"};

        public static string RemoveSpecialCharacters(string str) {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str) {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_') {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        
        private static ISwitchMediator SwitchMediator { get; set; }
        private static IWeatherDataMediator WeatherDataMediator { get; set; }
        private static ICameraMediator CameraMediator { get; set; }
        private static IDomeMediator DomeMediator { get; set; }
        private static IFlatDeviceMediator FlatMediator { get; set; }
        private static IFilterWheelMediator FilterWheelMediator { get; set; }
        private static IProfileService ProfileService {  get; set; }
        private static IRotatorMediator RotatorMediator { get; set; }
        private static ISafetyMonitorMediator SafetyMonitorMediator { get; set; }


        private static ConditionWatchdog ConditionWatchdog { get; set; }
        private static IList<string> Switches {  get; set; } = new List<string>();

        public static void InitMediators(ISwitchMediator switchMediator, IWeatherDataMediator weatherDataMediator, ICameraMediator cameraMediator, IDomeMediator domeMediator,
            IFlatDeviceMediator flatMediator, IFilterWheelMediator filterWheelMediator, IProfileService profileService, IRotatorMediator rotatorMediator, ISafetyMonitorMediator safetyMonitorMediator) {
            SwitchMediator = switchMediator;
            WeatherDataMediator = weatherDataMediator;
            CameraMediator = cameraMediator;
            DomeMediator = domeMediator;
            FlatMediator = flatMediator;
            FilterWheelMediator = filterWheelMediator;
            ProfileService = profileService;
            RotatorMediator = rotatorMediator;
            SafetyMonitorMediator = safetyMonitorMediator;
            ConditionWatchdog = new ConditionWatchdog(UpdateSwitchWeatherData, TimeSpan.FromSeconds(10));
            ConditionWatchdog.Start();
        }

        public static Keys SwitchWeatherKeys { get; set; } = new Keys();

        public static Keys GetSwitchWeatherKeys () {
            lock(SwitchMediator) {
                return SwitchWeatherKeys;
            }
        }

        public static IList<string> GetSwitches() {
            lock (SwitchMediator) {
                return Switches;
            }
        }

        public static Symbol.SymbolDictionary DataSymbols { get; set; } = new Symbol.SymbolDictionary();

        public Dictionary<string, Symbol> GetDataSymbols() {
            lock (SwitchMediator) {
                return DataSymbols;
            }
        }

        public static void AddSymbolData(string id, double value) {
            if (DataSymbols.ContainsKey(id)) {

            }
        }

        public static void UpdateDataSymbols() {
            lock (SwitchMediator) {
                DomeInfo domeInfo = DomeMediator.GetInfo();
                if (domeInfo.Connected) {
                    AddSymbolData("ShutterStatus", (int)domeInfo.ShutterStatus);
                    AddSymbolData("ShutterNone", -1);
                    AddSymbolData("ShutterOpen", 0);
                }
            }
        }

        public static Task UpdateSwitchWeatherData() {
            lock (SwitchMediator) {
                var i = new List<string>();
                SwitchWeatherKeys = new Keys();

                //SwitchWeatherKeys.Add("TIME", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                TimeSpan time = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
                double timeSeconds = Math.Floor(time.TotalSeconds);
                SwitchWeatherKeys.Add("TIME", timeSeconds);
                //i.Add("TIME: " + DateTime.Now.ToString("MM/dd/yyyy h:mm tt"));
                i.Add("TIME: " + timeSeconds);

                SafetyMonitorInfo safetyInfo = SafetyMonitorMediator.GetInfo();
                if (safetyInfo.Connected) {
                    SwitchWeatherKeys.Add("IsSafe", safetyInfo.IsSafe);
                    i.Add("Safety: IsSafe (" + safetyInfo.IsSafe + ")");
                }

                // Get SensorTemp
                CameraInfo cameraInfo = CameraMediator.GetInfo();
                if (cameraInfo.Connected) {
                    SwitchWeatherKeys.Add("SensorTemp", cameraInfo.Temperature);
                    i.Add("Camera: SensorTemp (" + cameraInfo.Temperature + ")");
                }

                DomeInfo domeInfo = DomeMediator.GetInfo();
                if (domeInfo.Connected) {
                    SwitchWeatherKeys.Add("ShutterStatus", (int)domeInfo.ShutterStatus);
                    i.Add("Dome: ShutterStatus (" + domeInfo.ShutterStatus + ")");
                    SwitchWeatherKeys.Add("ShutterNone", -1);
                    SwitchWeatherKeys.Add("ShutterOpen", 0);
                    SwitchWeatherKeys.Add("ShutterClosed", 1);
                    SwitchWeatherKeys.Add("ShutterOpening", 2);
                    SwitchWeatherKeys.Add("ShutterClosing", 3);
                    SwitchWeatherKeys.Add("ShutterError", 4);
                }

                FlatDeviceInfo flatInfo = FlatMediator.GetInfo();
                if (flatInfo.Connected) {
                    SwitchWeatherKeys.Add("CoverState", (int)flatInfo.CoverState);
                    i.Add("Flat Panel: CoverState (Cover" + flatInfo.CoverState + ")");
                    SwitchWeatherKeys.Add("CoverUnknown", 0);
                    SwitchWeatherKeys.Add("CoverNeitherOpenNorClosed", 1);
                    SwitchWeatherKeys.Add("CoverClosed", 2);
                    SwitchWeatherKeys.Add("CoverOpen", 3);
                    SwitchWeatherKeys.Add("CoverError", 4);
                }

                RotatorInfo rotatorInfo = RotatorMediator.GetInfo();
                if (rotatorInfo.Connected) {
                    SwitchWeatherKeys.Add("RotatorMechanicalPosition", rotatorInfo.MechanicalPosition);
                    i.Add("Rotator: RotatorMechanicalPosition (" + rotatorInfo.MechanicalPosition + ")");
                }

                FilterWheelInfo filterWheelInfo = FilterWheelMediator.GetInfo();
                if (filterWheelInfo.Connected) {
                    var f = ProfileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                    foreach (FilterInfo filterInfo in f) {
                        SwitchWeatherKeys.Add("Filter_" + RemoveSpecialCharacters(filterInfo.Name), filterInfo.Position);
                    }

                    SwitchWeatherKeys.Add("CurrentFilter", filterWheelInfo.SelectedFilter.Position);
                    i.Add("Filter Wheel: CurrentFilter (Filter_" + RemoveSpecialCharacters(filterWheelInfo.SelectedFilter.Name) + ")");

                }

                // Get switch values
                SwitchInfo switchInfo = SwitchMediator.GetInfo();
                if (switchInfo.Connected) {
                    foreach (ISwitch sw in switchInfo.ReadonlySwitches) {
                        string key = RemoveSpecialCharacters(sw.Name);
                        SwitchWeatherKeys.TryAdd(key, sw.Value);
                        i.Add("Gauge: " + key + " (" + sw.Value + ")");
                    }
                    foreach (ISwitch sw in switchInfo.WritableSwitches) {
                        string key = RemoveSpecialCharacters(sw.Name);
                        SwitchWeatherKeys.TryAdd(key, sw.Value);
                        i.Add("Switch: " + key + " (" + sw.Value + ")");
                    }
                }

                // Get weather values
                WeatherDataInfo weatherInfo = WeatherDataMediator.GetInfo();
                if (weatherInfo.Connected) {
                    foreach (string dataName in WeatherData) {
                        double t = weatherInfo.TryGetPropertyValue(dataName, Double.NaN);
                        if (!Double.IsNaN(t)) {
                            t = Math.Round(t, 2);
                            string key = RemoveSpecialCharacters(dataName);
                            SwitchWeatherKeys.TryAdd(key, t);
                            i.Add("Weather: " + key + " (" + t + ")");
                        }
                    }
                }

                Keys imageKeys = TakeExposure.LastImageResults;
                if (imageKeys != null) {
                    foreach (KeyValuePair<string, object> kvp in imageKeys) {
                        SwitchWeatherKeys.TryAdd(kvp.Key, kvp.Value);
                        i.Add("Last Image: " + kvp.Key + " (" + kvp.Value + ")");
                    }
                } else {
                    SwitchWeatherKeys.TryAdd("HFR", Double.NaN);
                    SwitchWeatherKeys.TryAdd("StarCount", Double.NaN);
                    SwitchWeatherKeys.TryAdd("FWHM", Double.NaN);
                    SwitchWeatherKeys.TryAdd("Eccentricity", Double.NaN);
                    i.Add("No image data");
                }

                Switches = i;
                return Task.CompletedTask;
            }
        }
    }
}
