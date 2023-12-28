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
using System.Windows.Controls;
using System.Windows.Data;

namespace WhenPlugin.When {
  
    [JsonObject(MemberSerialization.OptIn)]

    public abstract class Symbol : SequenceItem, IValidatable {

        public class SymbolDictionary : Dictionary<string, Symbol> { public static explicit operator Dictionary<object, object>(SymbolDictionary v) { throw new NotImplementedException(); } };

        public static Dictionary<ISequenceContainer, SymbolDictionary> SymbolCache = new Dictionary<ISequenceContainer, SymbolDictionary>();

        public static Dictionary<Symbol, List<string>> Orphans = new Dictionary<Symbol, List<string>>();
        
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

        static public bool IsAttachedToRoot(ISequenceEntity item) {
            if (item.Parent == null) return false;
            return IsAttachedToRoot (item.Parent);
        }

        // Must prevent cycles
        public static void SymbolDirty (Symbol sym) {
            List<Symbol> dirtyList = new List<Symbol>();
            iSymbolDirty(sym, dirtyList);
        }

        public static void iSymbolDirty(Symbol sym, List<Symbol> dirtyList) {
            Debug.WriteLine("SymbolDirty: " + sym);
            dirtyList.Add(sym);
            // Mark everything in the chain dirty
            foreach (Expr consumer in sym.Consumers) {
                consumer.ReferenceRemoved(sym);
                Symbol consumerSym = consumer.ExprSym;
                if (!consumer.Dirty && consumerSym != null) {
                    if (!dirtyList.Contains(consumerSym)) {
                        iSymbolDirty(consumerSym, dirtyList);
                    }
                }
                consumer.Dirty = true;
                consumer.Evaluate();
            }
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Debug.WriteLine("APC: " + this + ", New Parent = " + ((Parent == null) ? "null" : Parent.Name));
            if (!IsAttachedToRoot(Parent)) {
                if (Expr != null) {
                    // Clear out orphans of this Symbol
                    Orphans.Remove(this);
                    // We've deleted this Symbol
                    SymbolDictionary cached;
                    if (LastParent == null) {
                        Warn("Removed symbol " + this + " has no LastParent?");
                        // We're saving a template?
                        return;
                    }
                    if (SymbolCache.TryGetValue(LastParent, out cached)) {
                        if (cached.Remove(Identifier)) {
                            SymbolDirty(this);
                         } else {
                            Warn("Deleting " + this + " but not in Parent's cache?");
                        }
                    } else {
                       Warn("Deleting " + this + " but Parent has no cache?");
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
                    foreach (Expr consumer in Consumers) {
                        consumer.RemoveParameter(Identifier);
                    }
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
                if (Parent == null) {
                    _identifier = value;
                    return;
                }

                SymbolDictionary cached = null;
                if (value == _identifier || value.Length == 0) {
                    return;
                } else if (_identifier.Length != 0) {
                    // If there was an old value, remove it from Parent's dictionary
                    if (SymbolCache.TryGetValue(Parent, out cached)) {
                        cached.Remove(_identifier);
                        SymbolDirty(this);
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
                //ConstantExpression.GetSwitchWeatherKeys();
                context = context.Parent;
            }
            return null;
        }

        public static void ShowSymbols(object sender) {
            TextBox tb = (TextBox)sender;
            BindingExpression be = tb.GetBindingExpression(TextBox.TextProperty);
            Expr exp = be.ResolvedSource as Expr;

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
                tb.ToolTip = "No symbols used in this expression";
                return;
            }
            StringBuilder sb = new StringBuilder(cnt == 1 ? "Symbol: " : "Symbols: ");

            foreach (Symbol sym in syms.Values) {
                sb.Append(sym.Identifier.ToString());
                sb.Append(" (in ");
                sb.Append(sym.Parent.Name);
                sb.Append(") = ");
                sb.Append(sym.Expr.Error != null ? sym.Expr.Error : sym.Expr.Value.ToString());
                if (--cnt > 0) sb.Append("; ");
            }
            tb.ToolTip = sb.ToString();
        }


        public abstract bool Validate();

        public override string ToString() {
            return $"Symbol: Identifier {Identifier}, in {Parent.Name} with value {Expr.Value}";
        }

        // DEBUGGING

        public static void ShowSymbols () {
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
