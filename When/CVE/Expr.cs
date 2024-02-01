using Accord.Statistics.Models.Regression.Fitting;
using NCalc;
using NCalc.Domain;
using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.Sequencer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using static WhenPlugin.When.Symbol;
using Expression = NCalc.Expression;

namespace WhenPlugin.When {
    [JsonObject(MemberSerialization.OptIn)]
    public class Expr : BaseINPC {

        public Expr (string exp, Symbol sym) {
            Symbol = sym;
            SequenceEntity = sym;
            Expression = exp;
        }

        public Expr(ISequenceEntity item, string expression) {
            SequenceEntity = item;
            Expression = expression;
        }
        public Expr(ISequenceEntity item) {
            SequenceEntity = item;
        }

        public Expr(ISequenceEntity item, string expression, string type) {
            SequenceEntity = item;
            Expression = expression;
            Type = type;
        }

        public Expr(ISequenceEntity item, string expression, string type, Action<Expr> setter) {
            SequenceEntity = item;
            // SETTER MUST BE BEFORE EXPRESSION!!
            Setter = setter;
            Expression = expression;
            Type = type;
        }

        public Expr(ISequenceEntity item, string expression, string type, Action<Expr> setter, double def) {
            SequenceEntity = item;
            // SETTER and DEFAULT MUST BE BEFORE EXPRESSION!!
            Setter = setter;
            Default = def;
            Expression = expression;
            Type = type;
        }

        public Expr (Expr cloneMe) : this(cloneMe.SequenceEntity, cloneMe.Expression, cloneMe.Type) {
            Setter = cloneMe.Setter;
            Symbol = cloneMe.Symbol;
        }

        private string _expression = ""; 

        [JsonProperty]
        public string Expression {
            get => _expression;
            set {
                if (value == null) return;
                    value = value.Trim();
                if (value.Length == 0) {
                    IsExpression = false;
                    if (!double.IsNaN(Default)) {
                        Value = Default;
                    } else {
                        Value = Double.NaN;
                    }
                    _expression = value;
                    Parameters.Clear();
                    Resolved.Clear();
                    References.Clear();
                    return;
                }
                Double result;
                
                if (value != _expression && IsExpression) {
                    // The value has changed.  Clear what we had...cle
                    foreach (var symKvp in Resolved) {
                        Symbol s = symKvp.Value;
                        if (s != null) {
                            symKvp.Value.RemoveConsumer(this);
                        }
                    }
                    Resolved.Clear();
                    Parameters.Clear();
                }

                _expression = value;
                if (Double.TryParse(value, out result)) {
                    Error = null;
                    IsExpression = false;
                    Value = result;
                    // Notify consumers
                    if (Symbol != null) {
                        SymbolDirty(Symbol);
                    } else {
                        // We always want to show the result if not a Symbol
                        //IsExpression = true;
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
                    if (Symbol != null) SymbolDirty(Symbol);
                }
                RaisePropertyChanged("Expression");
                RaisePropertyChanged("IsAnnotated");
            }
        }

        private Double iDefault = Double.NaN;
        public Double Default {
            get => iDefault;
            set {
                iDefault = value;
                RaisePropertyChanged("Default");
                RaisePropertyChanged("Value");
                RaisePropertyChanged("ValueString");
            }
        }

        public Symbol Symbol { get; set; } = null;
        public ISequenceEntity SequenceEntity { get; set; } = null;

        [JsonProperty]
        public string Type { get; set; } = null;


        public Action<Expr> Setter { get; set; }



        
        private static Dictionary<string, object> EmptyDictionary = new Dictionary<string, object> ();

        private double _value = Double.NaN;
        public double Value {
            get {
                if (double.IsNaN(_value) && !double.IsNaN(Default)) {
                    return Default;
                }
                return _value;
            }
            set {
                if (value != _value) {
                    if ("Integer".Equals(Type)) {
                        value = Double.Floor(value);
                    }
                    _value = value;
                    if (Setter != null) {
                        Setter(this);
                    }
                    RaisePropertyChanged("ValueString");
                    RaisePropertyChanged("IsExpression");
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
                    RaisePropertyChanged("IsExpression");
                    RaisePropertyChanged("IsAnnotated");
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

        public bool IsAnnotated {
            get => IsExpression || Error != null;
            set { }
        }

        // References are the parsed tokens used in the Expr
        public HashSet<string> References { get; set; } = new HashSet<string>();

        // Resolved are the Symbol's that have been found (from the References)
        public Dictionary<string, Symbol> Resolved = new Dictionary<string, Symbol>();
        
        // Parameters are NCalc Parameters used in the call to NCalc.Evaluate()
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
            Resolved.Remove(identifier);
            Evaluate();
        }

        public bool Dirty { get; set; } = false;

        public bool Volatile {  get; set; } = false;
        public bool ImageVolatile {  get; set; } = false;

        public void DebugWrite() {
            Debug.WriteLine("* Expression " + Expression + " evaluated to " + ((Error != null) ? Error : Value) + " (in " + (Symbol != null ? Symbol : SequenceEntity) + ")");
        }

        public void ReferenceRemoved (Symbol sym) {
            // A definition we use was removed
            string identifier = sym.Identifier;
            Parameters.Remove(identifier);
            Resolved.Remove(identifier);
            Evaluate();
        }

        public static string NOT_DEFINED = "Parameter was not defined (Parameter";

        private void Resolve(string reference, Symbol sym) {
            Parameters.Remove(reference);
            Resolved.Remove(reference);
            if (sym.Expr.Error == null && !Double.IsNaN(sym.Expr.Value)) {
                Resolved.Add(reference, sym);
                Parameters.Add(reference, sym.Expr.Value);

            }
        }

        public void Refresh () {
            Parameters.Clear();
            Resolved.Clear();
            Evaluate();
        }

        public void Evaluate() {
            if (!IsExpression) return;
            if (Expression.Length == 0) {
                // How the hell to clear the Expr
                IsExpression = false;
                RaisePropertyChanged("Value");
                RaisePropertyChanged("ValueString");
                RaisePropertyChanged("IsExpression");
                return;
            }
            if (SequenceEntity == null) return;
            if (!Symbol.IsAttachedToRoot(SequenceEntity)) {
               return;
            }
            //Debug.WriteLine("Evaluate " + this);
            Dictionary<string, object> DataSymbols = Symbol.GetSwitchWeatherKeys();

            Volatile = false;
            ImageVolatile = false;

            // First, validate References
            foreach (string sRef in References) {
                Symbol sym;
                // Take care of "by reference" arguments
                string symReference = sRef;
                if (symReference.StartsWith("_")) {
                    symReference = sRef.Substring(1);
                }
                // Remember if we have any image data
                if (!ImageVolatile && symReference.StartsWith("Image_")) {
                    ImageVolatile = true;
                }
                bool found = Resolved.TryGetValue(symReference, out sym);
                if (!found || sym == null) {
                    // !found -> couldn't find it; sym == null -> it's a DataSymbol
                    if (!found) {
                        sym = Symbol.FindSymbol(symReference, SequenceEntity.Parent);
                    }
                    if (sym != null) {
                        // Link Expression to the Symbol
                        Resolve(symReference, sym);
                        sym.AddConsumer(this);
                    } else {
                        SymbolDictionary cached;
                        found = false;
                        if (SymbolCache.TryGetValue(WhenPluginObject.Globals, out cached)) {
                            Symbol global;
                            if (cached != null && cached.TryGetValue(symReference, out global)) {
                                Resolve(symReference, global);
                                global.AddConsumer(this);
                                found = true;
                            }
                        }
                        // Try in the old Switch/Weather keys
                        object Val;
                        if (!found && DataSymbols.TryGetValue(symReference, out Val)) {
                            // We don't want these resolved, just added to Parameters
                            Resolved.Remove(symReference);
                            Resolved.Add(symReference, null);
                            Parameters.Remove(symReference);
                            Parameters.Add(symReference, Val);
                            Volatile = true;
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
                if (Symbol != null) {
                    Orphans.Remove(Symbol);
                    Orphans.Add(Symbol, orphans);
                }
            }

            Error = null;
            try {
                if (Parameters.Count != References.Count) {
                    foreach (string r in References) {
                        if (!Parameters.ContainsKey(r)) {
                            // Not defined or evaluated
                            Symbol s = FindSymbol(r, SequenceEntity.Parent);
                            if (s is SetVariable sv && !sv.Executed) {
                                Error = "Not evaluated: " + r;
                            } else {
                                Error = "Undefined: " + r;
                            }
                        }
                    }
                    RaisePropertyChanged("ValueString");
                    RaisePropertyChanged("Value");
                } else {
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
                }

            } catch (ArgumentException ex) {
                string error = ex.Message;
                // Shorten this common error from NCalc
                int pos = error.IndexOf(NOT_DEFINED);
                if (pos == 0) {
                    error = "Undefined: " + error.Substring(NOT_DEFINED.Length).TrimEnd(')');
                }
                Error = error;
            } catch (NCalc.EvaluationException) {
                Error = "Syntax Error";
                return;
            } catch (Exception ex) {
                Logger.Warning("Exception evaluating" + Expression + ": " + ex.Message);
            }
            Dirty = false;
        }

        public void Validate(IList<string> issues) {
            if (Error != null || Volatile) {
                if (Expression != null && Expression.Length == 0 && Value == Default) {
                    Error = null;
                }
                Evaluate();
            } else if (Double.IsNaN(Value)) {
                Error = "Not evaluated";
            } else if (Expression.Length != 0 && Value == Default && Error == null) {
                // This seems very wrong to me; need to figure it out
                Evaluate();
            }
        }

        public void Validate() {
            Validate(null);
        }

        public void NotNegative (Expr expr) {
            if (expr.Value < 0) {
                expr.Error = "Must not be negative";
            }
        }

        public override string ToString() {
            string id = Symbol != null ? Symbol.Identifier : SequenceEntity.Name;
            if (Error != null) {
                return $"Expr: '{Expression}' in {id}, References: {References.Count}, Error: {Error}";
            } else if (Expression.Length == 0) {
                return "Expr: None";
            }
            return $"Expr: Expression: {Expression} in {id}, References: {References.Count}, Value: {Value}";
        }
    }
}

