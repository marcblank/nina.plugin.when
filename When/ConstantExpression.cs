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
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Linq;

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
        
        static private Dictionary<ISequenceContainer, Keys> KeyCache = new Dictionary<ISequenceContainer, Keys>();  
        
        private class Keys : Dictionary<string, object> {

            public override string ToString() {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, object> kvp in this) {
                    sb.Append(kvp.Key);
                    sb.Append(' ');
                }
                return sb.ToString();
            }
        }
       
        static private Dictionary<string, object> CopyKeys(Dictionary<string, object> dict) {
            Dictionary<string, object> copy = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> keyValuePair in dict) {
                copy.Add(keyValuePair.Key, keyValuePair.Value);
            }
            return copy;
        }

        private static Stack<Keys> KeysStack = new Stack<Keys>();

        private static int FC = 0;

        static private void FindConstantsRoot(ISequenceContainer container, Keys keys) {
            Debug.WriteLine("Root: #" + ++FC);
            FindConstants(container, keys);
        }

        static private double EvaluateExpression (string expr, Stack<Keys> stack) {
            Expression e = new Expression(expr);
            // Consolidate keys
            Keys mergedKeys = new Keys();
            foreach (Keys k in stack) {
                foreach (KeyValuePair<string, object> kvp in k) {
                    if (!mergedKeys.ContainsKey(kvp.Key)) {
                        mergedKeys.Add(kvp.Key, kvp.Value);
                    }
                }
            }
            e.Parameters = mergedKeys;
            try {
                var eval = e.Evaluate();
                Debug.WriteLine("Expression " + expr + " evaluated to " + eval);
                try {
                    return (double)eval;
                } catch (Exception ex) {
                    return Double.NaN;
                }
            } catch (Exception ex) {
                return Double.NaN;
            }

        }

        static private void FindConstants(ISequenceContainer container, Keys keys) {
            if (container == null) return;
            if (container.Items.IsNullOrEmpty()) return;

            Keys cachedKeys = null;
            if (KeyCache.TryGetValue(container, out cachedKeys)) {
                Debug.WriteLine("FindConstants for " + container.Name + " in cache: " + cachedKeys);
            } else {
                Debug.WriteLine("FindConstants: " + container.Name);
            }
 
            KeysStack.Push(keys);

            foreach (ISequenceItem item in container.Items) {
                if (item is SetConstant sc && cachedKeys == null) {
                    string name = sc.Constant;
                    string val = sc.ValueExpr;
                    double value;
                    if (Double.TryParse(val, out value)) {
                        // The value is a number, so we're good
                        keys.Add(name, value);
                        Debug.WriteLine("Constant " + name + " defined as " + value);
                    } else {
                        double result = EvaluateExpression(val, KeysStack);
                        if (result != Double.NaN) {
                            keys.Add(name, result);
                        }
                    }
                } else if (item is ISequenceContainer descendant && descendant.Items.Count > 0) {
                    FindConstants(descendant, new Keys());
                }
            }
            
            if (cachedKeys == null) {
                if (KeyCache.ContainsKey(container)) {
                    KeyCache.Remove(container);
                }
                KeyCache.Add(container, keys);
            }
            
            KeysStack.Pop();

            if (keys.Count > 0) {
                Debug.WriteLine(container.Name + ": " + keys);
            }
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
            if (root != null) {
                Debug.WriteLine("IsValid: ", item.Name + ", " + exprName + " = " + expr);
                FindConstantsRoot(root, new Keys());
            }
            
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
                    // Build the keys stack
                    Stack<Keys> stack = new Stack<Keys>();
                    ISequenceContainer cc = c;
                    while (cc != null) {
                        Keys cachedKeys;
                        KeyCache.TryGetValue(cc, out cachedKeys);
                        if (cachedKeys != null) {
                            stack.Push(cachedKeys);
                        }
                        cc = cc.Parent;
                    }
                    double result = EvaluateExpression(expr, stack);
                    if (result == Double.NaN) {
                        val = -1;
                        return false;
                    } else {
                        val = result;
                        return true;
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
