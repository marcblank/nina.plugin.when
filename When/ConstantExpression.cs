using Castle.Core.Internal;
using Namotion.Reflection;
using NCalc;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace WhenPlugin.When {
    public class ConstantExpression {
        static private bool AreConstantsResolved (string[] tokens, IList<string> issues, bool root) {
            double dbl;
            bool allFound = true;
            foreach (string tok in tokens) {
                if (!double.TryParse(tok, out dbl)) {
                    if (Regex.IsMatch(tok, "^[a-zA-Z0-9]*$")) {
                        // This is an alphanumeric string that isn't a number
                        if (root) {
                            issues?.Add(tok + " is not defined");
                        }
                        allFound = false;
                    }
                }
            }
            return allFound;
        }

        static private bool ResolveInContainer(SequenceItem exprItem, string exprName, ISequenceContainer container, string[] tokens, IList<string> issues) {
           foreach (SequenceItem item in container.Items) {
                if (item is SetConstant sc) {
                    string constantName = sc.Constant.ToLower();
                    for (int i = 0; i < tokens.Length; i++) {
                        string str = tokens[i];
                        if (string.Equals(str.Trim().ToLower(), constantName)) {
                            sc.AddConsumer(exprItem, exprName);
                            // TODO: Better test here...
                            if (sc.Value >= 0) {
                                tokens[i] = sc.Value.ToString();
                            }
                       }
                    }
                }
            }
            bool topLevel = container is SequenceRootContainer || container is StartAreaContainer;
            return AreConstantsResolved(tokens, issues, topLevel || (container.Parent == null));
        }

        static SequenceRootContainer FindRoot(ISequenceContainer cont) {
            while (cont != null) {
                if (cont is SequenceRootContainer root) { return root; }
                cont = cont.Parent;
            }
            return null;
        }
        
        static private Dictionary<ISequenceContainer, Dictionary<string, object>> KeyCache = new Dictionary<ISequenceContainer, Dictionary<string, object>>();  
        
        private class Keys : Dictionary<string, object> {
        }
        
        static private Dictionary<string, object> CopyKeys(Dictionary<string, object> dict) {
            Dictionary<string, object> copy = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> keyValuePair in dict) {
                copy.Add(keyValuePair.Key, keyValuePair.Value);
            }
            return copy;
        }

        private static Stack<Keys> KeysStack = new Stack<Keys>();
        
        static private void FindConstants(ISequenceContainer container, Keys keys) {
            if (container == null) return;
            if (container.Items.IsNullOrEmpty()) return;

            KeysStack.Push(keys);

            Debug.WriteLine("FindConstants: " + container.Name);
            foreach (ISequenceItem item in container.Items) {
                if (item is SetConstant sc) {
                    string name = sc.Constant;
                    string val = sc.ValueExpr;
                    double value;
                    if (Double.TryParse(val, out value)) {
                        // The value is a number, so we're good
                        keys.Add(name, value);
                    } else {
                        // The value is an expression, so we must deal with it
                        Expression e = new Expression(val);
                        // Consolidate keys
                        Keys mergedKeys = new Keys();
                        foreach(Keys k in KeysStack) {
                            foreach(KeyValuePair<string, object> kvp in k) {
                                if (!mergedKeys.ContainsKey(kvp.Key)) {
                                    mergedKeys.Add(kvp.Key, kvp.Value);
                                }
                            }
                        }
                        e.Parameters = mergedKeys;
                        try {
                            var eval = e.Evaluate();
                            keys.Add(name, eval);
                        } catch (Exception ex) {
                            Debug.WriteLine("OOPS!");
                        }

                    }
                } else if (item is ISequenceContainer descendant) {
                    FindConstants(descendant, new Keys());
                }
            }
            if (KeyCache.ContainsKey(container)) {
                KeyCache.Remove(container);
            }
            KeyCache.Add(container, keys);
            KeysStack.Pop();

            Debug.WriteLine(container.Name + ": " + keys);
        }

        public static bool IsValidExpression_new(SequenceItem item, string exprName, string expr, out double val, IList<string> issues) {
            val = 0;
            if (expr.IsNullOrEmpty()) return false;
            try {
                val = Double.Parse(expr);
                return true;

            } catch (Exception) {
                // Ok, it's not a number. Let's look for constants
                // Find relevant keys
                // Look in cache eventually
                Keys keys = new Keys();
                ISequenceContainer c = item.Parent;
                if (c != null) {
                    // Tokenize the expression
                    string[] tokens = Regex.Split(expr, @"(?=[-+*/])|(?<=[-+*/])");
                    List<string> tokenList = tokens.Cast<string>().ToList();
                    FindConstants(c, keys);
                }
            }
            return true;
        }

        
        public static bool IsValidExpression(SequenceItem item, string exprName, string expr, out double val, IList<string> issues) {
            val = 0;

            if (item.Parent == null) return true;
            
            ISequenceContainer root = FindRoot(item.Parent);
            FindConstants(root, new Keys());
            
            try {
               if (expr == null || expr.Length == 0) {
                    return false;
                }
                // Best case, this is a number of some sort
                val = double.Parse(expr);
                return true;
            } catch (Exception) {
                ISequenceContainer c = item.Parent;
                // Ok, it's not a number. Let's look for constants
                if (c != null) {                                      
                    // Tokenize the expression
                    string[] tokens = Regex.Split(expr, @"(?=[-+*/])|(?<=[-+*/])");

                    bool resolved = false;
                    while (c != null) {
                        if (ResolveInContainer(item, exprName, c, tokens, issues)) {
                            expr = string.Concat(tokens);
                            resolved = true;
                            break;
                        }
                        else {
                            c = c.Parent;
                        }
                    }
                    if (c == null) return false;

                    if (!resolved) {
                        // Look in the StartArea
                        // ** Check if we're already IN the start container!
                        c = item.Parent;
                        while (c != null) {
                            c = c.Parent;
                            if (c != null && c is SequenceRootContainer) {
                                // Found root container; now let's find the StartArea
                                foreach (SequenceItem rootItem in c.Items) {
                                    if (rootItem is StartAreaContainer) {
                                        ISequenceContainer startArea = (SequenceContainer)rootItem;
                                        // Don't pass issues because we've already failed to resolve
                                        if (ResolveInContainer(item, exprName, startArea, tokens, null)) { //issues)) {
                                            expr = string.Concat(tokens);
                                            resolved = true;
                                            break;
                                        } else {
                                            return false;
                                        }
                                    }
                                }
                            }
                            if (resolved) break;
                        }
                        return false;
                    }

                    if (!resolved) return false;
                    DataTable dt = new DataTable();
                    try {
                        var v = dt.Compute(expr, "");
                        if (v == DBNull.Value) {
                            issues.Add("Not an arithmetic expression");
                            return false;
                        }
                        else {
                            if (v is double) {
                                val = (double)v;
                                return true;
                            }
                            else {
                                val = Convert.ToDouble(v);
                                return true;
                            }
                        }
                    }
                    catch (Exception) {
                        // Not a valid expression
                    }

                }
            }
            return false;
        }

        public static bool Evaluate(SequenceItem item, string exprName, string valueName) {
            double val;
            string expr = item.TryGetPropertyValue(exprName, "") as string;

            if (ConstantExpression.IsValidExpression(item, exprName, expr, out val, null)) {
                PropertyInfo pi = item.GetType().GetProperty(valueName);
                try {
                    var conv = Convert.ChangeType(val, pi.PropertyType);
                    pi.SetValue(item, conv);
                    return true;
                } catch (Exception ex) {
                    pi.SetValue(item, 0);
                    throw new ArgumentException("Bad");
                }
            }
            return false;
        }

    }
}
