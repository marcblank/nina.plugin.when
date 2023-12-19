using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using NINA.Sequencer.Container;
using System.Text;
using NINA.Core.Utility;
using NINA.Sequencer;
using Google.Protobuf.WellKnownTypes;
using System.Diagnostics;
using System.Linq;
using System.ComponentModel;

namespace WhenPlugin.When {
  
    [JsonObject(MemberSerialization.OptIn)]

    public abstract class Symbol : SequenceItem, IValidatable {

        public class SymbolDictionary : Dictionary<string, Symbol> { public static explicit operator Dictionary<object, object>(SymbolDictionary v) { throw new NotImplementedException(); } };

        public static Dictionary<ISequenceContainer, SymbolDictionary> SymbolCache = new Dictionary<ISequenceContainer, SymbolDictionary>();

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

        private bool Debugging = true;

        public static void Warn (string str) {
            Logger.Warning (str);
        }

        private ISequenceContainer LastParent {  get; set; }

        static private bool IsAttachedToRoot(ISequenceContainer container) {
            ISequenceEntity p = container;
            while (p != null) {
                if (p is SequenceRootContainer) {
                    return true;
                } else {
                    p = p.Parent;
                }
            }
            return false;
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            if (!IsAttachedToRoot(Parent)) {
                if (Expr != null) {
                    // We've deleted this Symbol
                    SymbolDictionary cached;
                    if (LastParent == null) {
                        Warn("Removed symbol " + this + " has no LastParent?");
                    }
                    if (SymbolCache.TryGetValue(LastParent, out cached)) {
                        if (cached.Remove(Identifier)) {
                            foreach (Expr consumer in Consumers) {
                                consumer.ReferenceRemoved(this);
                                consumer.Evaluate();
                                ShowSymbols();
                            }
                        } else {
                            Warn("Deleting " + this + " but not in Parent's cache?");
                        }
                    } else {
                       Warn("De leting " + this + " but Parent has no cache?");
                    }
                }
                return;
            }
            LastParent = Parent;

            Expr = new Expr(Definition, this);

            try {
                if (Identifier != null && Identifier.Length == 0) return;
                SymbolDictionary cached;
                if (SymbolCache.TryGetValue(Parent, out cached)) {
                    cached.Add(Identifier, this);
                } else {
                    SymbolDictionary newSymbols = new SymbolDictionary {
                        { Identifier, this }
                    };
                    SymbolCache.Add(Parent, newSymbols);
                    // Can we see if the Parent moves?
                    // Parent.AfterParentChanged += ??
                }
            } catch (Exception ex) {
                Logger.Info("Ex");
            }
        }

        private string _identifier = "";

        [JsonProperty]
        public string Identifier {
            get => _identifier;
            set {
                SymbolDictionary cached = null;
                if (value == _identifier || value.Length == 0) {
                    return;
                } else if (_identifier.Length != 0) {
                    // If there was an old value, remove it from Parent's dictionary
                    if (SymbolCache.TryGetValue(Parent, out cached)) {
                        cached.Remove(_identifier);
                    }
                }
                
                _identifier = value;
                
                // Store the symbol in the SymbolCache for this Parent
                if (Parent != null) {
                    if (cached != null || SymbolCache.TryGetValue(Parent, out cached)) {
                        cached.Add(Identifier, this);
                    } else {
                        SymbolDictionary newSymbols = new SymbolDictionary();
                        SymbolCache.Add(Parent, newSymbols);
                        newSymbols.Add(Identifier, this);
                    }
                }
            }
        }

        private string _definition = "";
        
        [JsonProperty]
        public string Definition {
            get => _definition;
            set {
                if (value == _definition) {
                    return;
                }
                _definition = value;
                if (Parent != null) {
                    Expr.Expression = value;
                    if (Debugging) {
                        ShowSymbols();
                    }
                }
                RaisePropertyChanged("Expr");
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


        public IList<string> Issues => new List<string>();

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

        public HashSet<Expr> Consumers = new HashSet<Expr>();

        public void AddConsumer (Expr expr) {
            if (!Consumers.Contains(expr)) {
                Consumers.Add(expr);
            }
        }

        public void RemoveConsumer (Expr expr) {
            if (!Consumers.Remove(expr)) {
                Warn("RemoveConsumer: " + expr + " not found in " + this);
            }
        }

        public static Symbol FindSymbol(string identifier, ISequenceContainer context) {
            while (context != null) {
                SymbolDictionary cached;
                if (SymbolCache.TryGetValue(context, out cached)) {
                    if (cached.ContainsKey(identifier)) {
                        return cached[identifier];
                    }
                }
                ConstantExpression.GetSwitchWeatherKeys();
                context = context.Parent;
            }
            return null;
        }

        public abstract bool Validate();

        public override string ToString() {
            return $"Symbol: Identifier {Identifier}, in {Parent.Name} with value {Expr.Value}";
        }
        public static void ShowSymbols () {
            Debug.WriteLine("Symbols");
            foreach (var k in SymbolCache) {
                ISequenceContainer c = k.Key;
                SymbolDictionary syms = k.Value;
                Debug.WriteLine("Container: " + c.Name);
                foreach (var kv in syms) {
                    Debug.WriteLine("   " + kv.Key + " / " + kv.Value);
                    if (kv.Value.Consumers.Count> 0) {
                        foreach(Expr e in kv.Value.Consumers) {
                            Debug.WriteLine("        -> " + e.ExprSym);
                        }
                    }
                }
            }
        }


    }
}
