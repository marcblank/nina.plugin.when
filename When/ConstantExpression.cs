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
            DebugInfo("++ UpdateConstants for " + item);
            lock (ConstantsLock) {
                ISequenceContainer root = GetRoot(item);
                if (root != null) {
                    KeyCache.Clear();
                    FindConstantsRoot(root, new Keys());
                    if (Debugging) {
                        DebugInfo(" - Resulting in KeyCache: ", KeyCache.Count.ToString(), " **");
                        foreach (var kvp in KeyCache) {
                            DebugInfo(kvp.Key.Name, ": ", kvp.Value.ToString());
                            foreach (var c in kvp.Value) {
                                DebugInfo(c.Key, " = ", c.Value is ISequenceEntity ? "Undefined" : c.Value.ToString());
                            }
                        }
                    }
                } else if (item.Parent != null) {
                    FindConstants(GlobalContainer, new Keys());
                }
            }
        }

        static public SequenceContainer GlobalContainer = new SequentialContainer() { Name = "Global Constants" };

        static private bool InFlight { get; set; } = false;

        static private int FCDepth = 0;

        static private void FindConstantsRoot(ISequenceContainer container, Keys keys) {
            lock (ConstantsLock) {
                // We start from root, but we'll add global constants
                FCDepth++;
                DebugInfo(" - IN FindConstantsRoot: #", (++FC).ToString(), " Depth: ", FCDepth.ToString());
                if (!GlobalContainer.Items.Contains(container)) {
                    DebugInfo("Adding GlobalContainer...");
                    GlobalContainer.Items.Add(container);
                }
                FindConstants(GlobalContainer, keys);
                DebugInfo(" - OUT FindConstantsRoot: #", (FC).ToString(), " Depth: ", FCDepth.ToString());
                FCDepth--;
            }
        }

        static private Double NCalcEvaluate(string expr, Keys mergedKeys, IList<string> issues) {
            SEM.WaitOne();
            try {
                Expression e = new Expression(expr, EvaluateOptions.IgnoreCase);

                e.Parameters = mergedKeys;
                try {
                    DebugInfo("     ### Evaluating ", expr);
                    foreach (KeyValuePair<string, object> x in mergedKeys) {
                        //Logger.Info("Key: " + x.Key + ", Value: " +  x.Value);
                    }
                    var eval = e.Evaluate();
                    DebugInfo("     ### Expression '", expr, " evaluated to " + eval.ToString());
                    if (eval is Boolean b) {
                        return b ? 1 : 0;
                    }
                    try {
                        return Convert.ToDouble(eval);
                    } catch (Exception ex) {
                        return Double.NaN;
                    }
                } catch (Exception ex) {
                    if (issues != null) {
                        if (ex is EvaluationException) {
                            issues.Add("Syntax error");
                        } else {
                            issues.Add(ex.Message);
                        }
                    }
                    return Double.NaN;
                }
            } finally {
                SEM.Release();
            }

        }

        static private Double EvaluateExpression(ISequenceEntity item, string expr, Stack<Keys> stack, IList<string> issues) {
            lock (ConstantsLock) {
                if (string.IsNullOrEmpty(expr)) return 0;

                if (string.Equals(expr, "true", StringComparison.OrdinalIgnoreCase)) { return 1; }
                if (string.Equals(expr, "false", StringComparison.OrdinalIgnoreCase)) { return 0; }

                // Consolidate keys
                Keys mergedKeys = GetSwitchWeatherKeys().Clone();
                   
                foreach (Keys k in stack) {
                    foreach (KeyValuePair<string, object> kvp in k) {
                        object kvpValue = kvp.Value;
                        if (!mergedKeys.ContainsKey(kvp.Key)) {
                            Double kvpDouble;
                            if (kvpValue is Double d) {
                                kvpDouble = d;
                            } else {
                                // Value is a SetVariable instruction
                                SetVariable sv = kvpValue as SetVariable;
                                if (sv.Status == NINA.Core.Enum.SequenceEntityStatus.FINISHED) {
                                    IsValid(sv, sv.CValueExpr, sv.CValue, out kvpDouble, null);
                                } else {
                                    kvpDouble = Double.NaN;
                                }
                            }
                            if (!Double.IsNaN(kvpDouble)) {
                                mergedKeys.Add(kvp.Key, kvpDouble);
                            }
                        } else {
                            DebugInfo("&&& &&& merged key already contains ", kvp.Key, " = ", kvpValue.ToString());
                        }
                    }
                }
                
                if (mergedKeys.Count == 0) {
                    DebugInfo("Expression '", expr, "' not evaluated; no keys");
                    return Double.NaN;
                }

                return NCalcEvaluate(expr, mergedKeys, issues);
            }
        }

        static Keys GetMergedKeys(Stack<Keys> stack) {
            lock (ConstantsLock) {
                // Consolidate keys
                Keys mergedKeys = GetSwitchWeatherKeys().Clone(); // new Keys();

                foreach (Keys k in stack) {
                    foreach (KeyValuePair<string, object> kvp in k) {
                        if (!mergedKeys.ContainsKey(kvp.Key)) {
                            if (kvp.Value is SetVariable) {
                                continue;
                            }
                            if (!Double.IsNaN((double)kvp.Value)) {
                                mergedKeys.Add(kvp.Key, kvp.Value);
                            }
                        }
                    }
                }
                return mergedKeys;
            }
        }

        static public Keys GetParsedKeys(LogicalExpression e, Keys mergedKeys, Keys k) {
            lock (ConstantsLock) {
                if (e is BinaryExpression b) {
                    GetParsedKeys(b.LeftExpression, mergedKeys, k);
                    GetParsedKeys(b.RightExpression, mergedKeys, k);
                } else if (e is Identifier i) {
                    try {
                        k.Add(i.Name, mergedKeys.GetValueOrDefault(i.Name));
                    } catch (Exception ex) {
                        //Logger.Info("Key used multiple times: " + i.Name);
                    }
                } else if (e is TernaryExpression t) {
                    //Logger.Info("LI");
                } else if (e is Function f) {
                    if (f.Expressions != null) {
                        foreach (LogicalExpression ee in f.Expressions) {
                            GetParsedKeys(ee, mergedKeys, k);
                        }
                    }
                }
                return k;
            }
        }

        static public string FindKey(ISequenceEntity item, string key) {
            ISequenceEntity p = FindKeyContainer(item, key);
            if (p == null) {
                if (SwitchWeatherKeys.ContainsKey(key)) {
                    return "Device Data";
                }
                return "??";
            }
            return (p == item.Parent ? "Here" : p == GlobalContainer ? "Global" : p.Name);

        }

        static public ISequenceEntity FindKeyContainer(ISequenceEntity item, string key) {
            lock (ConstantsLock) {
                ISequenceContainer root = FindRoot(item.Parent);
                ISequenceEntity p = item.Parent;
                while (p != null) {
                    Keys k = KeyCache.GetValueOrDefault(p, null);
                    if (k != null) {
                        if (k.ContainsKey(key)) {
                            return p;
                        }
                    }
                    if (p == root) {
                        p = GlobalContainer;
                    } else if (p is IfContainer ifc) {
                        p = ifc.PseudoParent;
                    } else {
                        p = p.Parent;
                    }
                }
                return null;
            }
        }

        static public string DissectExpression(ISequenceEntity item, string expr, Stack<Keys> stack) {
            lock (ConstantsLock) {
                if (string.IsNullOrEmpty(expr)) return String.Empty;

                Expression e = new Expression(expr, EvaluateOptions.IgnoreCase);
                // Consolidate keys
                Keys mergedKeys = GetMergedKeys(stack);
                if (mergedKeys.Count == 0) {
                    DebugInfo("Expression ", expr, " not evaluated; no keys");
                    return String.Empty;
                }
                e.Parameters = mergedKeys;
                try {
                    var eval = e.Evaluate();
                    // Find the keys used in the expression
                    Keys parsedKeys = GetParsedKeys(e.ParsedExpression, mergedKeys, new Keys());
                    StringBuilder stringBuilder = new StringBuilder("");
                    int cnt = parsedKeys.Count;
                    if (cnt == 0) {
                        stringBuilder.Append("No constants or variables used");
                    } else {
                        foreach (var key in parsedKeys) {
                            string whereDefined = FindKey(item, key.Key);
                            stringBuilder.Append(key.Key + " (" + whereDefined + ") = " + key.Value);
                            if (--cnt > 0) stringBuilder.Append("; ");
                        }
                    }
                    return (stringBuilder.ToString());
                } catch (Exception ex) {
                    if (ex is EvaluationException) {
                        return ("Syntax error");
                    } else {
                        return ("Error: " + ex.Message);
                    }
                }
            }
        }

        static public Stack<Keys> GetKeyStack(ISequenceEntity item) {
            lock (ConstantsLock) {

                // Build the keys stack, walking up the ladder of Parents
                ISequenceContainer root = FindRoot(item.Parent);
                Stack<Keys> stack = new Stack<Keys>();
                ISequenceEntity cc = item.Parent;
                while (cc != null) {
                    Keys cachedKeys;
                    KeyCache.TryGetValue(cc, out cachedKeys);
                    if (!(cachedKeys == null || cachedKeys.Count == 0)) {
                        stack.Push(cachedKeys);
                    }
                    if (cc == root) {
                        cc = GlobalContainer;
                    } else {
                        cc = GetParent(cc);
                    }
                }

                stack.Push(GetSwitchWeatherKeys().Clone());

                // Reverse the stack to maintain proper scoping
                Stack<Keys> reverseStack = new Stack<Keys>();
                Keys k;
                while (stack.TryPop(out k)) {
                    reverseStack.Push(k);

                }
                return reverseStack;
            }
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
            lock (ConstantsLock) {

                if (!Loaded && (container != GlobalContainer)) {
                    DebugInfo("Not loaded and not GlobalContainer; returning");
                    return;
                }

                if (container == null) {
                    DebugInfo("Container is null; returning");
                    return;
                }

                if (container.Items == null || container.Items.Count == 0) {
                    DebugInfo("No items in container; returning");
                    return;
                }

                Keys cachedKeys = null;
                if (KeyCache.TryGetValue(container, out cachedKeys)) {
                    DebugInfo("FindConstants for '", container.Name, "' found in cache: ", cachedKeys.ToString());
                } else {
                    //DebugInfo("FindConstants for '", container.Name, "'");
                }

                KeysStack.Push(keys);

                foreach (ISequenceEntity item in container.Items) {
                    if (item is ISettable sc && cachedKeys == null) {
                        string name = sc.GetSettable();
                        string val = sc.GetValueExpression();
                        string typ = sc.GetType();
                        double value;
                        sc.IsDuplicate(false);
                        if (item.Parent != container) {
                            // In this case item has been deleted from parent (but it's still in Parent's Items)
                        } else if (string.IsNullOrEmpty(name)) {
                            //DebugInfo("Empty name in ", typ, "; ignore");
                        } else if (Double.TryParse(val, out value)) {
                            // The value is a number, so we're good
                            try {
                                keys.Add(name, value);
                            } catch (Exception) {
                                // Multiply defined...
                                sc.IsDuplicate(true);
                            }
                            DebugInfo(typ, "'", name, "' defined as ", value.ToString());
                        } else {
                            double result = EvaluateExpression(item, val, KeysStack, null);
                            if (!double.IsNaN(result) || item is SetVariable) {
                                try {
                                    if (item is SetVariable sv) {
                                        if (item.Status == NINA.Core.Enum.SequenceEntityStatus.FINISHED) {
                                            // If the SetVariable has been executed, use it's actual value
                                            DebugInfo("SetVariable '" + name + "' set to " + sv.CValue);
                                            Double d = Double.NaN;
                                            Double.TryParse(sv.CValue, out d);
                                            keys.Add(name, d);
                                        } else {
                                            // Otherwise, use the SetVariable itself
                                            keys.Add(name, sv);
                                        }
                                    } else {
                                        keys.Add(name, result);
                                    }
                                } catch (Exception) {
                                    // Multiply defined...
                                    sc.IsDuplicate(true);
                                }
                                DebugInfo(typ, "'", name, "': ", val, " evaluated to ", result.ToString());
                            } else {
                                DebugInfo(typ, "'", name, "' evaluated as NaN");
                            }
                        }
                    } else if (item is IfCommand ifc && ifc.Instructions.Items.Count > 0) {
                        FindConstants(ifc.Instructions, new Keys());
                        if (item is IfThenElse ec && ec.ElseInstructions.Items.Count > 0) {
                            FindConstants(ec.ElseInstructions, new Keys());
                        }
                    } else if (item is ISequenceContainer descendant && descendant.Items.Count > 0) {
                        FindConstants(descendant, new Keys());
                    }
                }

                if (cachedKeys == null) {
                    if (KeyCache.ContainsKey(container)) {
                        KeyCache.Remove(container, out _);
                    }
                    if (keys.Count > 0) {
                        KeyCache.TryAdd(container, keys);
                    }
                }

                KeysStack.Pop();

                if (keys.Count > 0) {
                    DebugInfo("Constants defined in '", container.Name, "': ", keys.ToString());
                }
            }
        }

        private static bool Loaded { get; set; } = false;

        public static bool IsValid(object obj, string exprName, string expr, out double val, IList<string> issues) {
            lock (ConstantsLock) {
                val = 0;
                ISequenceEntity item = obj as ISequenceEntity;
                if (item == null || item.Parent == null || expr == null || expr.Length == 0) {
                    return false;
                }

                DebugInfo(" @@ IsValid: ", exprName, ":", expr);

                // We will always process the Global container
                if (item.Parent != GlobalContainer) {
                    if (!IsAttachedToRoot(item.Parent)) return true;
                    // Say that we have a sequence loaded...
                    Loaded = true;
                }
                // Best case, this is a number a some sort
                if (double.TryParse(expr, out val)) {
                    DebugInfo("IsValid for ", item.Name, ": '", exprName, "' = ", expr);
                    return true;
                }


                // Make sure we're up-to-date on constants
                ISequenceContainer parent = item.Parent;
                ISequenceContainer root = FindRoot(parent);
                Keys kk;
                if (root != null && (KeyCache == null || KeyCache.Count == 0 || (KeyCache.Count == 1 && KeyCache.TryGetValue(GlobalContainer, out kk)))) {
                    UpdateConstants(item);
                } else if (!(parent is IImmutableContainer) && !KeyCache.ContainsKey(parent)) {
                    // The IImmutableContainer case is for TakeManyExposures and SmartExposure, which are containers and items
                    UpdateConstants(item);
                }

                ISequenceContainer c = item.Parent;
                if (c != null) {
                    // Build the keys stack, walking up the ladder of Parents
                    Stack<Keys> stack = new Stack<Keys>();
                    ISequenceEntity cc = c;
                    while (cc != null) {
                        Keys cachedKeys;
                        KeyCache.TryGetValue(cc, out cachedKeys);
                        if (!(cachedKeys == null || cachedKeys.Count == 0)) {
                            stack.Push(cachedKeys);
                        }
                        if (cc is SequenceRootContainer) {
                            cc = GlobalContainer;
                        } else {
                            cc = GetParent(cc);
                        }
                    }

                    // Reverse the stack to maintain proper scoping
                    Stack<Keys> reverseStack = new Stack<Keys>();
                    Keys k;
                    while (stack.TryPop(out k)) {
                        reverseStack.Push(k);
                    }

                    //if ((reverseStack == null || reverseStack.Count == 0) && issues != null) issues.Add("There are no valid constants defined.");

                    double result = EvaluateExpression(item, expr, reverseStack, issues);
                    DebugInfo("IsValid: ", item.Name, ", ", exprName, " = ", expr,
                        ((issues == null || issues.Count == 0) ? (" (" + result + ")") : " issue: " + issues[0]));
                    if (Double.IsNaN(result)) {
                        val = -1;
                        return false;
                    } else {
                        val = result;
                        return true;
                    }
                }
                return false;
            }
        }

        public static bool Evaluate(ISequenceEntity item, string exprName, string valueName, object def) {
            lock (ConstantsLock) {
                return Evaluate(item, exprName, valueName, def, null);
            }
        }

        public static bool Evaluate(ISequenceEntity item, string exprName, string valueName, object def, IList<string> issues) {
            lock (ConstantsLock) {
                double val;
                string expr = item.TryGetPropertyValue(exprName, "") as string;

                PropertyInfo pi = item.GetType().GetProperty(valueName);
                if (IsValid(item, exprName, expr, out val, issues)) {
                    try {
                        var conv = Convert.ChangeType(val, pi.PropertyType);
                        pi.SetValue(item, conv);
                        return true;
                    } catch (Exception) {
                    }
                }
                try {
                    pi.SetValue(item, def);
                } catch (Exception) {
                    try {
                        var conv = Convert.ChangeType(def, pi.PropertyType);
                        pi.SetValue(item, conv);
                    } catch (Exception ex) {
                        DebugInfo("Caught exception: ", ex.Message);
                        Logger.Info("Caught exception: " + ex);
                    }
                }
                return false;
            }
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


        private static ConditionWatchdog ConditionWatchdog { get; set; }
        private static IList<string> Switches {  get; set; } = new List<string>();

        public static void InitMediators(ISwitchMediator switchMediator, IWeatherDataMediator weatherDataMediator, ICameraMediator cameraMediator, IDomeMediator domeMediator,
            IFlatDeviceMediator flatMediator, IFilterWheelMediator filterWheelMediator, IProfileService profileService, IRotatorMediator rotatorMediator) {
            SwitchMediator = switchMediator;
            WeatherDataMediator = weatherDataMediator;
            CameraMediator = cameraMediator;
            DomeMediator = domeMediator;
            FlatMediator = flatMediator;
            FilterWheelMediator = filterWheelMediator;
            ProfileService = profileService;
            RotatorMediator = rotatorMediator;
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

        public static Task UpdateSwitchWeatherData() {
            lock (SwitchMediator) {
                var i = new List<string>();
                SwitchWeatherKeys.Clear();

                //SwitchWeatherKeys.Add("TIME", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                TimeSpan time = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
                double timeSeconds = Math.Floor(time.TotalSeconds);
                SwitchWeatherKeys.Add("TIME", timeSeconds);
                //i.Add("TIME: " + DateTime.Now.ToString("MM/dd/yyyy h:mm tt"));
                i.Add("TIME: " + timeSeconds);

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
