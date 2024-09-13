using Newtonsoft.Json;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using NINA.Sequencer.Container;
using System.Text;
using NINA.Core.Utility;
using NINA.Sequencer;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Equipment.Equipment.MyFilterWheel;
using Namotion.Reflection;
using System.IO;
using System.Linq;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Astrometry.Interfaces;
using System.Collections.Concurrent;
using NINA.Astrometry;
using Accord;
using NCalc.Domain;

namespace WhenPlugin.When {

    [JsonObject(MemberSerialization.OptIn)]

    public abstract class Symbol : SequenceItem, IValidatable {

        public class SymbolDictionary : ConcurrentDictionary<string, Symbol> { public static explicit operator ConcurrentDictionary<object, object>(SymbolDictionary v) { throw new NotImplementedException(); } };

        public static ConcurrentDictionary<ISequenceContainer, SymbolDictionary> SymbolCache = new ConcurrentDictionary<ISequenceContainer, SymbolDictionary>();

        public static ConcurrentDictionary<Symbol, List<string>> Orphans = new ConcurrentDictionary<Symbol, List<string>>();

        [ImportingConstructor]
        public Symbol() {
            Name = Name;
            Icon = Icon;
        }

        public Symbol(Symbol copyMe) : this() {
            if (copyMe != null) {
                CopyMetaData(copyMe);
                Name = copyMe.Name;
                Icon = copyMe.Icon;
                Identifier = copyMe.Identifier;
                Definition = copyMe.Definition;
            }
        }

        static public SequenceContainer GlobalContainer = new SequentialContainer() { Name = "Global Constants" };

        static public SequenceContainer GlobalVariables = new SequentialContainer() { Name = "Global Variables" };

        public bool IsGlobalVariable { get; set; } = false;

        public class Keys : Dictionary<string, object>;

        public static readonly String VALID_SYMBOL = "^[a-zA-Z][a-zA-Z0-9-+_]*$";

        public bool IsDuplicate { get; private set; } = false;

        public static void Warn(string str) {
            Logger.Warning(str);
        }

        private ISequenceContainer LastSParent { get; set; }

        static private bool IsAttachedToRoot(ISequenceContainer container) {
            ISequenceEntity p = container;
            while (p != null) {
                if (p is SequenceRootContainer || p == WhenPluginObject.Globals) {
                    return true;
                } else {
                    p = p.Parent;
                }
            }
            return false;
        }

        static public bool IsAttachedToRoot(ISequenceEntity item) {
            if (item.Parent == null) return false;
            return IsAttachedToRoot(item.Parent);
        }

        // Must prevent cycles
        public static void SymbolDirty(Symbol sym) {
            if (Debugging) {
                Logger.Info("SymbolDirty: " + sym);
            }
            List<Symbol> dirtyList = new List<Symbol>();
            iSymbolDirty(sym, dirtyList);
        }

        public static void iSymbolDirty(Symbol sym, List<Symbol> dirtyList) {
            Debug.WriteLine("SymbolDirty: " + sym);
            dirtyList.Add(sym);
            // Mark everything in the chain dirty
            foreach (var consumer in sym.Consumers) {
                Expr expr = consumer.Key;
                expr.ReferenceRemoved(sym);
                Symbol consumerSym = expr.Symbol;
                if (!expr.Dirty && consumerSym != null) {
                    if (!dirtyList.Contains(consumerSym)) {
                        iSymbolDirty(consumerSym, dirtyList);
                    }
                }
                expr.Dirty = true;
                expr.Evaluate();
            }
        }

        private string GenId(SymbolDictionary dict, string id) {
            for (int i = 0; ; i++) {
                string newId = id + "_" + i.ToString();
                if (!dict.ContainsKey(newId)) {
                    return newId;
                }
            }
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            ISequenceContainer sParent = SParent();
            if (sParent == LastSParent) {
                return;
            }
            Debug.WriteLine("APC: " + this + ", New Parent = " + ((sParent == null) ? "null" : sParent.Name));
            if (!IsAttachedToRoot(Parent) && (Parent != WhenPluginObject.Globals) && !(this is SetGlobalVariable)) {
                if (Expr != null) {
                    // Clear out orphans of this Symbol
                    Orphans.TryRemove(this, out _);
                    // We've deleted this Symbol
                    SymbolDictionary cached;
                    if (LastSParent == null) {
                        Warn("Removed symbol " + this + " has no LastSParent?");
                        // We're saving a template?
                        return;
                    }
                    if (SymbolCache.TryGetValue(LastSParent, out cached)) {
                        if (cached.TryRemove(Identifier, out _)) {
                            SymbolDirty(this);
                        } else {
                            Warn("Deleting " + this + " but not in SParent's cache?");
                        }
                    } else {
                        Warn("Deleting " + this + " but SParent has no cache?");
                    }
                }
                return;
            }
            LastSParent = sParent;

            Expr = new Expr(Definition, this);

            try {
                if (Identifier != null && Identifier.Length == 0) return;
                SymbolDictionary cached;
                if (SymbolCache.TryGetValue(sParent, out cached)) {
                    if (cached.TryAdd(Identifier, this)) {
                        if (Debugging) {
                            Logger.Info("APC: Added " + Identifier + " to " + sParent.Name);
                        }
                    } else {
                        if (sParent != WhenPluginObject.Globals) {
                            IsDuplicate = true;
                            Identifier = GenId(cached, Identifier);
                            cached.TryAdd(Identifier, this);
                        }
                    }
                } else {
                    SymbolDictionary newSymbols = new SymbolDictionary();
                    newSymbols.TryAdd(Identifier, this);
                    SymbolCache.TryAdd(sParent, newSymbols);
                    if (Debugging) {
                        Logger.Info("APC: Added " + sParent.Name + " to SymbolCache");
                        Logger.Info("APC: Added " + Identifier + " to " + sParent.Name);
                    }

                    foreach (var consumer in Consumers) {
                        consumer.Key.RemoveParameter(Identifier);
                    }
                    // Can we see if the Parent moves?
                    // Parent.AfterParentChanged += ??
                }
            } catch (Exception ex) {
                Logger.Error("Exception in Symbol evaluation: " + ex.Message);
            }
        }

        protected static bool Debugging = false;
        
        private string _identifier = "";

        [JsonProperty]
        public string Identifier {
            get => _identifier;
            set {
                if (Parent == null) {
                    _identifier = value;
                    return;
                }

                ISequenceContainer sParent = SParent();

                SymbolDictionary cached = null;
                if (value == _identifier) {
                    return;
                } else if (_identifier.Length != 0) {
                    // If there was an old value, remove it from Parent's dictionary
                    if (!IsDuplicate && SymbolCache.TryGetValue(sParent, out cached)) {
                        if (Debugging) {
                            Logger.Info("Removing " + _identifier + " from " + sParent.Name);
                        }
                        cached.TryRemove(_identifier, out _);
                        SymbolDirty(this);
                    }
                }

                _identifier = value;

                if (value.Length == 0) return;

                // Store the symbol in the SymbolCache for this Parent
                if (Parent != null) {
                    if (cached != null || SymbolCache.TryGetValue(sParent, out cached)) {
                        if (cached.TryAdd(Identifier, this)) {
                            if (Debugging) {
                                Logger.Info("Adding " + Identifier + " to " + sParent.Name);
                            }
                        } else {
                            string oldIdentifier = Identifier;
                            _identifier = GenId(cached, Identifier);
                            Logger.Warning("Attempt to add " + oldIdentifier + " multiple times to " + sParent.Name + "; using " + Identifier);
                            cached.TryAdd(Identifier, this);
                        }
                    } else {
                        SymbolDictionary newSymbols = new SymbolDictionary();
                        if (Debugging) {
                            Logger.Info("Creating new SymbolCache entry for " + this.Name);
                        }
                        SymbolCache.TryAdd(sParent, newSymbols);
                        newSymbols.TryAdd(Identifier, this);
                    }
                }

                if (this is SetConstant constant && constant.GlobalName != null) {
                    constant.SetGlobalName(Identifier);
                }
            }
        }

        private string _definition = "";

        [JsonProperty]
        public string Definition {
            get => _definition;
            set {
                if (value == _definition) {
                    if (Expr != null && value != Expr.Expression) {
                        Logger.Warning("Definition not reflected in Expression; user changed value manually");
                    } else {
                        return;
                    }
                }
                _definition = value;
                if (SParent != null) {
                    if (Expr != null) {
                        if (Debugging) {
                            Logger.Info("Setting Definition for " + Identifier + " in " + SParent().Name + ": " + value);
                        }
                        Expr.Expression = value;
                    }
                }
                RaisePropertyChanged("Expr");

                if (this is SetConstant constant && constant.GlobalValue != null) {
                    constant.SetGlobalValue(value);
                }

            }
        }

        private Expr _expr = null;
        public Expr Expr {
            get => _expr;
            set {
                _expr = value;
                RaisePropertyChanged();
            }
        }

        public IList<string> Issues { get; set; }

        public bool IsReference { get; set; } = false;

        protected bool IsAttachedToRoot() {
            ISequenceContainer p = Parent;
            while (p != null) {
                if (p is SequenceRootContainer) {
                    return true;
                }
                p = p.Parent;
            }
            return false;
        }

        public ConcurrentDictionary<Expr, byte> Consumers = new ConcurrentDictionary<Expr, byte>();
        public static WhenPlugin WhenPluginObject { get; set; }

        public ISequenceContainer SParent() {
            if (Parent == null) {
                return null;
            } else if (this is SetGlobalVariable) {
                return GlobalVariables;
            } else if (Parent is CVContainer cvc) {
                if (cvc.Parent is TemplateContainer tc) {
                    return tc.Parent;
                } else {
                    return cvc.Parent;
                }
            } else {
                return Parent;
            }
        }


        public void AddConsumer(Expr expr) {
            if (!Consumers.ContainsKey(expr)) {
                Consumers.TryAdd(expr, 0);
            }
        }

        public void RemoveConsumer(Expr expr) {
            if (!Consumers.TryRemove(expr, out _)) {
                Warn("RemoveConsumer: " + expr + " not found in " + this);
            }
        }

        public static Symbol FindSymbol(string identifier, ISequenceContainer context) {
            return FindSymbol(identifier, context, false);
        }

        public static Symbol FindSymbol(string identifier, ISequenceContainer context, bool includeGlobal) {
            while (context != null) {
                SymbolDictionary cached;
                if (SymbolCache.TryGetValue(context, out cached)) {
                    if (cached.ContainsKey(identifier)) {
                        if (Debugging) {
                            Logger.Info("FindSymbol '" + identifier + "' returning " + cached[identifier]);
                        }
                        return cached[identifier];
                    }
                }
                context = context.Parent;
            }
            if (includeGlobal) return FindGlobalSymbol(identifier);
            if (Debugging) {
                Logger.Info("FindSymbol '" + identifier + "' returning null");
            }
            return null;
        }

        protected static void DumpSymbols () {
            foreach (var c in SymbolCache) {
                Logger.Info("\r\nIn SymbolCache for " + c.Key.Name);
                foreach (var d in c.Value) {
                    Logger.Info("  -- " + d.Key + " / " + d.Value.ToString());
                }
            }
        }

        public static Symbol FindGlobalSymbol(string identifier) {
            SymbolDictionary cached;
            Symbol global = null;
            if (SymbolCache.TryGetValue(GlobalVariables, out cached)) {
                if (cached.ContainsKey(identifier)) {
                    global = cached[identifier];
                }
            }
            if (global is SetGlobalVariable) return global;
            return null;
        }

        public static void ShowSymbols(object sender) {
            TextBox tb = (TextBox)sender;
            BindingExpression be = tb.GetBindingExpression(TextBox.TextProperty);
            Expr exp = be.ResolvedSource as Expr;
            Dictionary<string, object> DataSymbols = Symbol.GetSwitchWeatherKeys();

            if (exp == null) {
                Symbol s = be.ResolvedSource as Symbol;
                if (s != null) {
                    exp = s.Expr;
                } else {
                    tb.ToolTip = "??";
                    return;
                }
            }

            Dictionary<string, Symbol> syms = exp.Resolved;
            int cnt = syms.Count;
            if (cnt == 0) {
                if (exp.References.Count == 1) {
                    tb.ToolTip = "The symbol is not yet defined";
                } else {
                    tb.ToolTip = "No defined symbols used in this expression";
                }
                return;
            }
            StringBuilder sb = new StringBuilder(cnt == 1 ? "Symbol: " : "Symbols: ");

            foreach (var kvp in syms) {
                Symbol sym = kvp.Value as Symbol;
                sb.Append(kvp.Key.ToString());
                if (sym != null) {
                    sb.Append(" (in ");
                    sb.Append(sym.SParent().Name);
                    ISequenceContainer sParent = sym.SParent();
                    if (sParent != sym.Parent) {
                        if (sym.Parent is CVContainer) {
                            sb.Append("/" + sym.Parent.Name);
                            if (sym.Parent.Parent is TemplateContainer tc) {
                                sb.Append("/TBR");
                                if (tc.PseudoParent != null && tc.PseudoParent is TemplateByReference tbr) {
                                    sb.Append("-" + tbr.TemplateName);
                                }
                            }
                        } else {
                            sb.Append(" - WTF");
                        }
                    }
                    sb.Append(") = ");
                    sb.Append(sym.Expr.Error != null ? sym.Expr.Error : sym.Expr.ValueString);
                } else {
                    // We're a data value
                    sb.Append(" (Data) = ");
                    sb.Append(DataSymbols.GetValueOrDefault(kvp.Key, "??"));
                }
                if (--cnt > 0) sb.Append("; ");
            }

            tb.ToolTip = sb.ToString();
        }
        public static string ShowSymbols(Expr exp) {
            Dictionary<string, object> DataSymbols = Symbol.GetSwitchWeatherKeys();

            if (exp == null) {
                return "??";
            }

            Dictionary<string, Symbol> syms = exp.Resolved;
            int cnt = syms.Count;
            if (cnt == 0) {
                if (exp.References.Count == 1) {
                    return "The symbol is not yet defined\r\n";
                } else {
                    return "No defined symbols used in this expression\r\n";
                }
            }
            StringBuilder sb = new StringBuilder(cnt == 1 ? "Symbol: " : "Symbols: ");

            foreach (var kvp in syms) {
                Symbol sym = kvp.Value as Symbol;
                sb.Append(kvp.Key.ToString());
                if (sym != null) {
                    sb.Append(" (in ");
                    sb.Append(sym.SParent().Name);
                    ISequenceContainer sParent = sym.SParent();
                    if (sParent != sym.Parent) {
                        if (sym.Parent is CVContainer) {
                            sb.Append("/" + sym.Parent.Name);
                            if (sym.Parent.Parent is TemplateContainer tc) {
                                sb.Append("/TBR");
                                if (tc.PseudoParent != null && tc.PseudoParent is TemplateByReference tbr) {
                                    sb.Append("-" + tbr.TemplateName);
                                }
                            }
                        } else {
                            sb.Append(" - WTF");
                        }
                    }
                    sb.Append(") = ");
                    sb.Append(sym.Expr.Error != null ? sym.Expr.Error : sym.Expr.Value.ToString());
                } else {
                    // We're a data value
                    sb.Append(" (Data) = ");
                    sb.Append(DataSymbols.GetValueOrDefault(kvp.Key, "??"));
                }
                if (--cnt > 0) sb.Append("; ");
            }

            sb.Append("\r\n");
            return sb.ToString();
        }


        public abstract bool Validate();

        public override string ToString() {
            return $"Symbol: Identifier {Identifier}, in {SParent()?.Name} with value {Expr.Value}";
        }


        // DATA SYMBOLS


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
        private static IProfileService ProfileService { get; set; }
        private static IRotatorMediator RotatorMediator { get; set; }
        private static ISafetyMonitorMediator SafetyMonitorMediator { get; set; }
        private static IFocuserMediator FocuserMediator { get; set; }
        private static ITelescopeMediator TelescopeMediator { get; set; }


        private static ConditionWatchdog ConditionWatchdog { get; set; }
        private static IList<string> Switches { get; set; } = new List<string>();

        public static void InitMediators(ISwitchMediator switchMediator, IWeatherDataMediator weatherDataMediator, ICameraMediator cameraMediator, IDomeMediator domeMediator,
            IFlatDeviceMediator flatMediator, IFilterWheelMediator filterWheelMediator, IProfileService profileService, IRotatorMediator rotatorMediator, ISafetyMonitorMediator safetyMonitorMediator,
            IFocuserMediator focuserMediator, ITelescopeMediator telescopeMediator) {
            SwitchMediator = switchMediator;
            WeatherDataMediator = weatherDataMediator;
            CameraMediator = cameraMediator;
            DomeMediator = domeMediator;
            FlatMediator = flatMediator;
            FilterWheelMediator = filterWheelMediator;
            ProfileService = profileService;
            RotatorMediator = rotatorMediator;
            SafetyMonitorMediator = safetyMonitorMediator;
            FocuserMediator = focuserMediator;
            TelescopeMediator = telescopeMediator;

            ConditionWatchdog = new ConditionWatchdog(UpdateSwitchWeatherData, TimeSpan.FromSeconds(5));
            ConditionWatchdog.Start();
        }

        public static Keys SwitchWeatherKeys { get; set; } = new Keys();

        public static Keys GetSwitchWeatherKeys() {
            lock (SYMBOL_LOCK) {
                return SwitchWeatherKeys;
            }
        }

        public static IList<string> GetSwitches() {
            lock (SYMBOL_LOCK) {
                return Switches;
            }
        }

        public static Symbol.SymbolDictionary DataSymbols { get; set; } = new Symbol.SymbolDictionary();

        public ConcurrentDictionary<string, Symbol> GetDataSymbols() {
            lock (SYMBOL_LOCK) {
                return DataSymbols;
            }
        }

        public static void AddSymbolData(string id, double value) {
            if (DataSymbols.ContainsKey(id)) {

            }
        }

        public static int LastExitCode { get; set; } = 0;

        private static bool TelescopeConnected = false;
        private static bool DomeConnected = false;
        private static bool SafetyConnected = false;
        private static bool FocuserConnected = false;
        private static bool CameraConnected = false;
        private static bool FlatConnected = false;
        private static bool FilterWheelConnected = false;
        private static bool RotatorConnected = false;
        private static bool SwitchConnected = false;
        private static bool WeatherConnected = false;

        public static bool SwitchWeatherConnectionStatusCurrent() {
            long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (TelescopeConnected != TelescopeMediator.GetInfo().Connected) { return false; }
            if (DomeConnected != DomeMediator.GetInfo().Connected) { return false; }
            if (SafetyConnected != SafetyMonitorMediator.GetInfo().Connected) { return false; }
            if (FocuserConnected != FocuserMediator.GetInfo().Connected) { return false; }
            if (CameraConnected != CameraMediator.GetInfo().Connected) { return false; }
            if (FlatConnected != FlatMediator.GetInfo().Connected) { return false; }
            if (FilterWheelConnected != FilterWheelMediator.GetInfo().Connected) { return false; }
            if (RotatorConnected != RotatorMediator.GetInfo().Connected) { return false; }
            if (SwitchConnected != SwitchMediator.GetInfo().Connected) { return false; }
            if (WeatherConnected != WeatherDataMediator.GetInfo().Connected) { return false; }
            return true;
        }

        private static ObserverInfo Observer = null;

        public static Object SYMBOL_LOCK = new object();

        
        private static HashSet<string> LoggedOnce = new HashSet<string>();
        public static void LogOnce (string message) {
            if (LoggedOnce.Contains(message)) return;
            Logger.Warning(message);
            LoggedOnce.Add(message);
        }

        public static Task UpdateSwitchWeatherData() {

            lock (SYMBOL_LOCK) {
                var i = new List<string>();
                SwitchWeatherKeys = new Keys();

                if (Observer == null) {
                    Observer = new ObserverInfo() {
                        Latitude = ProfileService.ActiveProfile.AstrometrySettings.Latitude,
                        Longitude = ProfileService.ActiveProfile.AstrometrySettings.Longitude
                    };
                }

                double moonAltitude = AstroUtil.GetMoonAltitude(DateTime.UtcNow, Observer);
                SwitchWeatherKeys.Add("MoonAltitude", moonAltitude);
                i.Add(" MoonAltitude: " + Math.Round(moonAltitude, 2));
                
                double sunAltitude = AstroUtil.GetSunAltitude(DateTime.UtcNow, Observer);
                SwitchWeatherKeys.Add("SunAltitude", sunAltitude);
                i.Add(" SunAltitude: " + Math.Round(sunAltitude, 2));

                double lst = AstroUtil.GetLocalSiderealTimeNow(ProfileService.ActiveProfile.AstrometrySettings.Longitude);
                SwitchWeatherKeys.Add("LocalSiderealTime", lst);
                i.Add(" LocalSiderealTime: " + Math.Round(lst, 4));

                TimeSpan time = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
                double timeSeconds = Math.Floor(time.TotalSeconds);
                SwitchWeatherKeys.Add("TIME", timeSeconds);
                i.Add(" TIME: " + timeSeconds);

                SwitchWeatherKeys.Add("EXITCODE", LastExitCode);
                i.Add(" EXITCODE: " + LastExitCode);

                TelescopeInfo telescopeInfo = TelescopeMediator.GetInfo();
                TelescopeConnected = telescopeInfo.Connected;
                if (TelescopeConnected) {
                    SwitchWeatherKeys.Add("Altitude", telescopeInfo.Altitude);
                    i.Add("Telescope: Altitude (" + Math.Round(telescopeInfo.Altitude, 2) + ")");
                    SwitchWeatherKeys.Add("Azimuth", telescopeInfo.Azimuth);
                    i.Add("Telescope: Azimuth (" + Math.Round(telescopeInfo.Azimuth, 2) + ")");
                    SwitchWeatherKeys.Add("AtPark", telescopeInfo.AtPark ? 1 : 0);
                    i.Add("Telescope: AtPark (" + (telescopeInfo.AtPark ? "True" : "False") + ")");
                    Coordinates c = telescopeInfo.Coordinates.Transform(Epoch.J2000);
                    Double ra = c.RA;
                    Double dec = c.Dec;
                    SwitchWeatherKeys.Add("RightAscension", ra); // telescopeInfo.RightAscension);
                    i.Add("Telescope: RightAscension (" + Math.Round(ra, 2) + ")");
                    SwitchWeatherKeys.Add("Declination", dec); // telescopeInfo.Declination);
                    i.Add("Telescope: Declination (" + Math.Round(dec, 2) + ")");
                }

                SafetyMonitorInfo safetyInfo = SafetyMonitorMediator.GetInfo();
                SafetyConnected = safetyInfo.Connected;
                if (SafetyConnected) {
                    SwitchWeatherKeys.Add("IsSafe", safetyInfo.IsSafe);
                    i.Add("Safety: IsSafe (" + safetyInfo.IsSafe + ")");
                }

                string roofStatus = WhenPluginObject.RoofStatus;
                string roofOpenString = WhenPluginObject.RoofOpenString;
                if (roofStatus?.Length > 0 && roofOpenString?.Length > 0) {
                    SwitchWeatherKeys.Add("RoofOpen", 1);
                    SwitchWeatherKeys.Add("RoofNotOpen", 0);
                    SwitchWeatherKeys.Add("RoofCannotOpenOrRead", 2);
                    // It's actually a file name..
                    int status = 0;
                    try {
                        var lastLine = File.ReadLines(roofStatus).Last();
                        if (lastLine.ToLower().Contains(roofOpenString.ToLower())) {
                            status = 1;
                        }
                    } catch (Exception e) {
                        LogOnce("Roof status, error: " + e.Message);
                        status = 2;
                    }
                    SwitchWeatherKeys.Add("RoofStatus", status);
                    i.Add("Roof: RoofStatus (" + (status == 0 ? "RoofNotOpen" : status == 1 ? "RoofOpen" : "RoofCannotOpenOrRead") + ")");
                }

                FocuserInfo fInfo = FocuserMediator.GetInfo();
                FocuserConnected = fInfo.Connected;
                if (fInfo != null && FocuserConnected) {
                    SwitchWeatherKeys.Add("FocuserPosition", fInfo.Position);
                    SwitchWeatherKeys.Add("FocuserTemperature", fInfo.Temperature);
                    i.Add("Focuser: FocuserPosition (" + fInfo.Position + ")");
                    i.Add("Focuser: FocuserTemperature (" + fInfo.Temperature + ")");
                }

                // Get SensorTemp
                CameraInfo cameraInfo = CameraMediator.GetInfo();
                CameraConnected = cameraInfo.Connected;
                if (CameraConnected) {
                    SwitchWeatherKeys.Add("SensorTemp", cameraInfo.Temperature);
                    i.Add("Camera: SensorTemp (" + cameraInfo.Temperature + ")");

                    // Hidden
                    SwitchWeatherKeys.Add("camera__PixelSize", cameraInfo.PixelSize);
                    SwitchWeatherKeys.Add("camera__XSize", cameraInfo.XSize);
                    SwitchWeatherKeys.Add("camera__YSize", cameraInfo.YSize);
                    SwitchWeatherKeys.Add("camera__CoolerPower", cameraInfo.CoolerPower);
                    SwitchWeatherKeys.Add("camera__CoolerOn", cameraInfo.CoolerOn);
                    SwitchWeatherKeys.Add("telescope__FocalLength", ProfileService.ActiveProfile.TelescopeSettings.FocalLength);
                }

                DomeInfo domeInfo = DomeMediator.GetInfo();
                DomeConnected = domeInfo.Connected;
                if (DomeConnected) {
                    SwitchWeatherKeys.Add("ShutterStatus", (int)domeInfo.ShutterStatus);
                    i.Add("Dome: ShutterStatus (" + domeInfo.ShutterStatus + ")");
                    SwitchWeatherKeys.Add("ShutterNone", -1);
                    SwitchWeatherKeys.Add("ShutterOpen", 0);
                    SwitchWeatherKeys.Add("ShutterClosed", 1);
                    SwitchWeatherKeys.Add("ShutterOpening", 2);
                    SwitchWeatherKeys.Add("ShutterClosing", 3);
                    SwitchWeatherKeys.Add("ShutterError", 4);
                    SwitchWeatherKeys.Add("DomeAzimuth", domeInfo.Azimuth);
                    i.Add("Dome: DomeAzimuth (" + Math.Round(domeInfo.Azimuth, 2) + ")");
              }

                FlatDeviceInfo flatInfo = FlatMediator.GetInfo();
                FlatConnected = flatInfo.Connected;
                if (FlatConnected) {                    SwitchWeatherKeys.Add("CoverState", (int)flatInfo.CoverState);
                    i.Add("Flat-Panel: CoverState (Cover" + flatInfo.CoverState + ")");
                    SwitchWeatherKeys.Add("CoverUnknown", 0);
                    SwitchWeatherKeys.Add("CoverNeitherOpenNorClosed", 1);
                    SwitchWeatherKeys.Add("CoverClosed", 2);
                    SwitchWeatherKeys.Add("CoverOpen", 3);
                    SwitchWeatherKeys.Add("CoverError", 4);
                }

                RotatorInfo rotatorInfo = RotatorMediator.GetInfo();
                RotatorConnected = rotatorInfo.Connected;
                if (RotatorConnected) {
                    SwitchWeatherKeys.Add("RotatorMechanicalPosition", rotatorInfo.MechanicalPosition);
                    SwitchWeatherKeys.Add("RotatorPosition", rotatorInfo.MechanicalPosition);
                    i.Add("Rotator: RotatorPosition (" + rotatorInfo.MechanicalPosition + ")");
                }

                FilterWheelInfo filterWheelInfo = FilterWheelMediator.GetInfo();
                FilterWheelConnected = filterWheelInfo.Connected;
                if (FilterWheelConnected) {
                    var f = ProfileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                    foreach (FilterInfo filterInfo in f) {
                        try {
                            SwitchWeatherKeys.Add("Filter_" + RemoveSpecialCharacters(filterInfo.Name), filterInfo.Position);
                        } catch (Exception ex) {
                            LogOnce("Exception trying to add filter '" + filterInfo.Name + "' in UpdateSwitchWeatherData");
                        }
                    }

                    if (filterWheelInfo.SelectedFilter != null) {
                        SwitchWeatherKeys.Add("CurrentFilter", filterWheelInfo.SelectedFilter.Position);
                        i.Add(" CurrentFilter (Filter_" + RemoveSpecialCharacters(filterWheelInfo.SelectedFilter.Name) + ")");
                    }
                }

                // Get switch values
                SwitchInfo switchInfo = SwitchMediator.GetInfo();
                SwitchConnected = switchInfo.Connected;
                if (SwitchConnected) {
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
                WeatherConnected = weatherInfo.Connected;
                if (WeatherConnected) {
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
                        var v = kvp.Value;
                        if (v is double d) {
                            v = Math.Round(d, 2);
                        }
                        i.Add(" Last Image: " + kvp.Key + " (" + v + ")");
                        Logger.Trace(" Last Image: " + kvp.Key + " (" + kvp.Value + ")");
                    }
                } else {
                    SwitchWeatherKeys.TryAdd("HFR", Double.NaN);
                    SwitchWeatherKeys.TryAdd("StarCount", Double.NaN);
                    SwitchWeatherKeys.TryAdd("FWHM", Double.NaN);
                    SwitchWeatherKeys.TryAdd("Eccentricity", Double.NaN);
                    //i.Add(" No image data");
                }

                Switches = i;

                WhenPluginDockable.UpdateData();

                return Task.CompletedTask;
            }
        }


    }
}
