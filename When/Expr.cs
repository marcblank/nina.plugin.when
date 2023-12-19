using CsvHelper;
using NCalc;
using NCalc.Domain;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using Nito.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                    foreach (var symKvp in Resolved) {
                        symKvp.Value.RemoveConsumer(this);
                    }
                    Resolved.Clear();
                    Parameters.Clear();
                }

                _expression = value;
                if (Double.TryParse(value, out result)) {
                    Value = result;
                    IsExpression = false;
                    // Notify consumers
                    Symbol.SymbolDirty(ExprSym);
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
                        Error = "Syntax Error";
                        return;
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
        public double Value {
            get => _value;
            set {
                if (value != _value) {
                    _value = value;
                    RaisePropertyChanged("ValueString");
                }
            }
        }
        
        private string _error;
        public string Error {
            get => _error;
            set {
                if (value != _error) {
                    _error = value;
                    RaisePropertyChanged("ValueString");
                }
            }
        }

        public string ValueString {
            get {
                if (Error != null) return Error;
                return Value.ToString();
            }
            set { }
        }

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

        public void RemoveParameter (string identifier) {
            Parameters.Remove(identifier);
            Evaluate();
        }

        
        public void MakeDirty () {
            Parameters.Clear();
            Evaluate();

        }
        public bool Dirty { get; set; } = false;

        public void DebugWrite() {
            Debug.WriteLine("* Expression " + Expression + " evaluated to " + ((Error != null) ? Error : Value) + " (in " + ExprSym + ")");
        }

        public void ReferenceRemoved (Symbol sym) {
            // A definition we use was removed
            string identifier = sym.Identifier;
            //Parameters.Remove(identifier);
            Resolved.Remove(identifier);
            Evaluate();
        }

        public static string NOT_DEFINED = "Parameter was not defined (Parameter";
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
                        Parameters.Remove(symReference);
                        Parameters.Add(symReference, sym.Expr.Value);
                        sym.AddConsumer(this);
                    }
                }
            }

            // Then evaluate
            Expression e = new Expression(Expression, EvaluateOptions.IgnoreCase);
            e.Parameters = Parameters;

            Error = null;
            try {
                object eval = e.Evaluate();
                // We got an actual value
                if (eval is Boolean b) {
                    Value = b ? 1 : 0;
                } else {
                    Value = Convert.ToDouble(eval);
                }
                Error = null;
                RaisePropertyChanged("Value");
                DebugWrite();

            } catch (ArgumentException ex) {
                // What kind of Exception is this??
                string error = ex.Message;
                // Shorten this common error from NCalc
                int pos = error.IndexOf(NOT_DEFINED);
                if (pos == 0) {
                    error = "Undefined: " + error.Substring(NOT_DEFINED.Length).TrimEnd(')');
                }
                Error = error;
                DebugWrite();
            } catch (Exception ex) {
                Logger.Warning("Exception evaluating" + Expression + ": " + ex.Message);
            }
            Dirty = false;
        }

        public override string ToString() {
            return $"Expr: Expression: {Expression} in {ExprSym.Identifier}, References: {References.Count}, Value: {Value}";
        }
    }
}

// Symbols go into a cache re: container
// Exprs have symbols attached

