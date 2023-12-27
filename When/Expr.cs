using CsvHelper;
using NCalc;
using NCalc.Domain;
using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using Nito.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static WhenPlugin.When.Symbol;

namespace WhenPlugin.When {
    [JsonObject(MemberSerialization.OptIn)]
    public class Expr : BaseINPC {

        public Expr (string exp, Symbol sym) {
            ExprSym = sym;
            ExprItem = sym;
            Expression = exp;
        }

        public Expr(SequenceItem item, string expression) {
            ExprSym = null;
            ExprItem = item;
            Expression = expression;
        }
        public Expr(SequenceItem item) {
            new Expr(item, "");
        }

        public Expr(SequenceItem item, string type, string validator) {
            ExprSym = null;
            ExprItem = item;
            Expression = "";
            ExprType = type;
            ExprValidator = validator;
        }

        private string _expression;
       [JsonProperty]
        public string Expression {
            get => _expression;
            set {
                if (value == null) return;
                if (value.Length == 0) {
                    IsExpression = false;
                    if (!double.IsNaN(Default)) {
                        Value = Default;
                    }
                    _expression = value;
                    return;
                }
                Double result;
                
                if (value != _expression && IsExpression) {
                    // The value has changed.  Clear what we had...cle
                    foreach (var symKvp in Resolved) {
                        symKvp.Value.RemoveConsumer(this);
                    }
                    Resolved.Clear();
                    Parameters.Clear();
                }

                _expression = value;
                if (Double.TryParse(value, out result)) {
                    Value = result;
                    Error = null;
                    IsExpression = false;
                    // Notify consumers
                    if (ExprSym != null) {
                        SymbolDirty(ExprSym);
                    } else {
                        // We always want to show the result if not a Symbol
                        IsExpression = true;
                    }
                } else if (Regex.IsMatch(value, "{(\\d+)}")) {
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
                    if (ExprSym != null) SymbolDirty(ExprSym);
                }
                RaisePropertyChanged("Expression");
                Notifier++;
            }
        }

        public Double Default { get; set; } = Double.NaN;
        
        public Symbol ExprSym { get; set; }
        public SequenceItem ExprItem {  get; set; }

        [JsonProperty]
        public string ExprType { get; set; } = null;

        [JsonProperty]
        public string ExprValidator { get; set; } = null;
        
        private static Dictionary<string, object> EmptyDictionary = new Dictionary<string, object> ();

        private double _value = 0; // Double.MinValue;
        public double Value {
            get => _value;
            set {
                if (value != _value) {
                    if ("Integer".Equals(ExprType)) {
                        value = Double.Floor(value);
                    }
                    _value = value;
                    RaisePropertyChanged("ValueString");
                    RaisePropertyChanged("Error");
                    RaisePropertyChanged("IsExpression");
                    Notifier = 0;
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
                    RaisePropertyChanged("Error");
                    RaisePropertyChanged("IsExpression");
                    Notifier = 0;
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

        private int iNotifier = 0;
        public int Notifier {
            get => iNotifier;
            set {
                iNotifier++;
                RaisePropertyChanged("Notifier");
            }
        }

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

        public bool Dirty { get; set; } = false;

        public void DebugWrite() {
            Debug.WriteLine("* Expression " + Expression + " evaluated to " + ((Error != null) ? Error : Value) + " (in " + (ExprSym != null ? ExprSym : ExprItem) + ")");
        }

        public void ReferenceRemoved (Symbol sym) {
            // A definition we use was removed
            string identifier = sym.Identifier;
            Parameters.Remove(identifier);
            Resolved.Remove(identifier);
            Evaluate();
        }

        public static string NOT_DEFINED = "Parameter was not defined (Parameter";
        public void Evaluate() {
            if (!IsExpression) return;
            if (ExprItem == null || !Symbol.IsAttachedToRoot(ExprItem)) return;
            Debug.WriteLine("Evaluate " + this);
            Dictionary<string, object> DataSymbols = ConstantExpression.GetSwitchWeatherKeys();

            // First, validate References
            foreach (string symReference in References) {
                if (!Resolved.ContainsKey(symReference)) {
                    // Find the symbol here or above
                    Symbol sym = Symbol.FindSymbol(symReference, ExprItem.Parent);
                    if (sym != null) {
                        // Link Expression to the Symbol
                        Parameters.Remove(symReference);
                        Resolved.Remove(symReference);
                        if (sym.Expr.Error == null) {
                            Resolved.Add(symReference, sym);
                            Parameters.Add(symReference, sym.Expr.Value);
                        }
                        sym.AddConsumer(this);
                    } else {
                        // Try in the old Switch/Weather keys
                        object Val;
                        if (DataSymbols.TryGetValue(symReference, out Val)) {
                            Parameters.Remove(symReference);
                            Parameters.Add(symReference, Val);
                        }

                    }
                }
            }

            // Then evaluate
            Expression e = new Expression(Expression, EvaluateOptions.IgnoreCase);
            e.Parameters = Parameters;

            if (Parameters.Count != References.Count) {
                // We have some undefineds...
                List<string> orphans = new List<string>();
                foreach (string r in References) {
                    if (!Resolved.ContainsKey(r)) {
                        // Try to find this; it might have been defined since last evaluation
                        orphans.Add(r);
                    }
                }
                // Save away our orphans in case they appear later
                if (ExprSym != null) {
                    Orphans.Remove(ExprSym);
                    Orphans.Add(ExprSym, orphans);
                }
            }

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
                RaisePropertyChanged("ValueString");
                RaisePropertyChanged("Value");
                DebugWrite();

            } catch (ArgumentException ex) {
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
            string id = ExprSym != null ? ExprSym.Identifier : ExprItem.Name;
            if (Error != null) {
                return $"Expr: Expression: {Expression} in {id}, References: {References.Count}, Error: {Error}";
            }
            return $"Expr: Expression: {Expression} in {id}, References: {References.Count}, Value: {Value}";
        }
    }
}

