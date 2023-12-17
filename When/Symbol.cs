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

namespace WhenPlugin.When {
  
    [JsonObject(MemberSerialization.OptIn)]

    public abstract class Symbol : SequenceItem, IValidatable {

        public static Dictionary<ISequenceContainer, List<Symbol>> SymbolCache = new Dictionary<ISequenceContainer, List<Symbol>>();

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
                if (Parent != null) {
                    List<Symbol> cached;
                    if (SymbolCache.TryGetValue(Parent, out cached)) {
                        cached.Add(this);
                    } else {
                        List<Symbol> newSymbols = [this];
                        SymbolCache.Add(Parent, newSymbols);
                    }
                }
            }
        }

        private string _identifier = "";

        [JsonProperty]
        public string Identifier {
            get => _identifier;
            set {
                if (value == _identifier || value.Length == 0) {
                    return;
                }
                _identifier = value;
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
                Expr = new Expr(Definition, Parent);
            }
        }

        private Expr _expr = null;
        public Expr Expr {
            get => _expr;
            set {
                _expr = value;
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

        public override void AfterParentChanged() {
            base.AfterParentChanged();
        }

        public abstract bool Validate();

    }
}
