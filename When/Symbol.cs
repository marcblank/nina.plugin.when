using NCalc;
using NCalc.Domain;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Expression = NCalc.Expression;

namespace WhenPlugin.When {
    public class Symbol {

        public Symbol (string name, Expr expr, ISequenceContainer context) {
            Expression = expr;
            Context = context;
        }

        private string _name;
        
        public string Name { get; set; }
        
        private Expr _expression;
        public Expr Expression {
            get => _expression;
            set {
            }
        }

        private ISequenceContainer Context { get; set; }

 

}
