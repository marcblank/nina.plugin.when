using CsvHelper;
using NCalc;
using NCalc.Domain;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using Nito.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WhenPlugin.When.Symbol;

namespace WhenPlugin.When {
    public class Expr : BaseINPC {

        public Expr (string exp, Symbol sym) {
            ExprSym = sym;
            Expression = exp;
        }

        private string _expression;
        public string Expression {
            get => _expression;
            set {
                if (value == null || value.Length == 0) return;
                Double result;
                
                if (value != _expression && IsExpression) {
                    // The value has changed.  Clear what we had...
                    Resolved.Clear();
                    // Remove references?
                    foreach (var symKvp in Resolved) {
                        symKvp.Value.RemoveConsumer(this);
                    }
                }
                
                _expression = value;
                if (Double.TryParse(value, out result)) {
                    Value = result;
                    IsExpression = false;
                } else {
                    IsExpression = true;
                    
                    // Evaluate just so that we can parse the expression
                    Expression e = new Expression(value, EvaluateOptions.IgnoreCase);
                    e.Parameters = EmptyDictionary;
                    IsSyntaxError = false;
                    try {
                        e.Evaluate();
                    } catch (NCalc.EvaluationException) {
                        // We should expect this, since we're just trying to find the parameters used
                        IsSyntaxError = true;
                    } catch (Exception) {
                        // That's ok
                    }

                    // Find the parameters used
                    var pe = e.ParsedExpression;
                    ParameterExtractionVisitor visitor = new ParameterExtractionVisitor();
                    pe.Accept(visitor);
                    
                    // References now holds all of the CV's used in the expression
                    References = visitor.Parameters;
                    Parameters.Clear();
                    Evaluate();
                 }
            }
        }

        private ISequenceContainer _ExprSym;
        public Symbol ExprSym { get; set; }
        
        private static Dictionary<string, object> EmptyDictionary = new Dictionary<string, object> ();

        private double _value;
        public double Value { get; set; } = 0;

        public bool IsExpression { get; set; } = false;

        public bool IsSyntaxError { get; set; } = false;

        public HashSet<string> References { get; set; } = new HashSet<string>();

        public Dictionary<string, Symbol> Resolved = new Dictionary<string, Symbol>();
        
        public Dictionary<string, object> Parameters = new Dictionary<string, object>();

        class ParameterExtractionVisitor : LogicalExpressionVisitor {
            public HashSet<string> Parameters = new HashSet<string>();

            public override void Visit(NCalc.Domain.Identifier function) {
                //Parameter - add to list
                Parameters.Add(function.Name);
            }

            public override void Visit(NCalc.Domain.UnaryExpression expression) {
                expression.Expression.Accept(this);
            }

            public override void Visit(NCalc.Domain.BinaryExpression expression) {
                //Visit left and right
                expression.LeftExpression.Accept(this);
                expression.RightExpression.Accept(this);
            }

            public override void Visit(NCalc.Domain.TernaryExpression expression) {
                //Visit left, right and middle
                expression.LeftExpression.Accept(this);
                expression.RightExpression.Accept(this);
                expression.MiddleExpression.Accept(this);
            }

            public override void Visit(Function function) {
                foreach (var expression in function.Expressions) {
                    expression.Accept(this);
                }
            }

            public override void Visit(LogicalExpression expression) {

            }

            public override void Visit(ValueExpression expression) {

            }
        }

        public bool Dirty { get; set; } = false;

        public void Evaluate() {
            if (!IsExpression) return;

            // First, validate References
            foreach (string symReference in References) {
                if (!Resolved.ContainsKey(symReference)) {
                    // Find the symbol here or above
                    Symbol sym = Symbol.FindSymbol(symReference, ExprSym.Parent);
                    if (sym != null) {
                        // Link Expression to the Symbol
                        Resolved.Add(symReference, sym);
                        Parameters.Add(symReference, sym.Expr.Value);
                        sym.AddConsumer(this);
                    }
                }
            }

            // Then evaluate
            Expression e = new Expression(Expression, EvaluateOptions.IgnoreCase);
            e.Parameters = Parameters;

            try {
                object eval = e.Evaluate();
                // We got an actual value
                if (eval is Boolean b) {
                    Value = b ? 1 : 0;
                } else {
                    Value = Convert.ToDouble(eval);
                }
                RaisePropertyChanged("Value");

            } catch (ArgumentException) {
                // What kind of Exception is this??
            } catch (Exception ex) {
                Logger.Warning("Exception evaluating" + Expression + ": " + ex.Message);
            }
        }

        public override string ToString() {
            return $"Expr: Expression: {Expression}, References: {References.Count}, Value: {Value}";
        }
    }
}

// Symbols go into a cache re: container
// Exprs have symbols attached

