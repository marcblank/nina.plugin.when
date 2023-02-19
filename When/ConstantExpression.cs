using Castle.Core.Internal;
using Namotion.Reflection;
using NCalc;
using NINA.Profile;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace WhenPlugin.When {
    public class ConstantExpression {
         static SequenceRootContainer FindRoot(ISequenceContainer cont) {
            while (cont != null) {
                if (cont is SequenceRootContainer root) { return root; }
                cont = cont.Parent;
            }
            return null;
        }
        
        static private ConcurrentDictionary<ISequenceContainer, Keys> KeyCache = new ConcurrentDictionary<ISequenceContainer, Keys>();
        
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
 
        private static Stack<Keys> KeysStack = new Stack<Keys>();

        private static int FC = 0;

        static public void FlushKeys() {
            KeyCache.Clear();
        }

        static public ISequenceContainer GetRoot(ISequenceItem item) {
            if (item == null) return null;
            ISequenceContainer p = item.Parent;
            while (p != null) {
                if (p is ISequenceRootContainer root) {
                    return root;
                }
                p = p.Parent;
            }
            return null;
        }
        
        static public void UpdateConstants(ISequenceItem item) {
            ISequenceContainer root = GetRoot(item);
            if (root != null) {
                KeyCache.Clear();
                FindConstantsRoot(root, new Keys());
                Debug.WriteLine("**KeyCache: " + KeyCache.Count + " **");
                foreach (var kvp in KeyCache) {
                    Debug.WriteLine(kvp.Key.Name + ": " + kvp.Value.ToString());
                    foreach (var c in kvp.Value) {
                        Debug.WriteLine(c);
                    }
                }
            } else {
                FindConstants(GlobalContainer, new Keys());
            }
        }

        static public SequenceContainer GlobalContainer = new SequentialContainer() { Name = "Global Constants" };

        static private bool InFlight { get; set; } = false;

        static private void FindConstantsRoot(ISequenceContainer container, Keys keys) {
            // We start from root, but we'll add global constants
            Debug.WriteLine("Root: #" + ++FC);
            if (!GlobalContainer.Items.Contains(container)) {
                GlobalContainer.Items.Add(container);
            }
            FindConstants(GlobalContainer, keys);
        }

        static private Double EvaluateExpression (ISequenceItem item, string expr, Stack<Keys> stack, IList<string> issues) {
            if (expr == null) return 0;

            Expression e = new Expression(expr);
            // Consolidate keys
            Keys mergedKeys = new Keys();

            foreach (Keys k in stack) {
                foreach (KeyValuePair<string, object> kvp in k) {
                    if (!mergedKeys.ContainsKey(kvp.Key)) {
                        if (!Double.IsNaN((double)kvp.Value)) {
                            mergedKeys.Add(kvp.Key, kvp.Value);
                        }
                    }
                }
            }
            if (mergedKeys.Count == 0) {
                Debug.WriteLine("Expression " + expr + " not evaluated; no keys");
                return Double.NaN;
            }
            e.Parameters = mergedKeys;
            try {
                var eval = e.Evaluate();
                Debug.WriteLine("Expression " + expr + " in " + item.Name + " evaluated to " + eval);
                try {
                    return (double)eval;
                } catch (Exception ex) {
                    return Double.NaN;
                }
            } catch (Exception ex) {
                if (issues != null) {
                    if (ex is EvaluationException) {
                        issues.Add("Syntax error");
                    } else {
                        issues.Add(ex.Message);
                    }
                }
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
                    string val = sc.CValueExpr;
                    double value;
                    if (name.IsNullOrEmpty()) {
                        //Debug.WriteLine("Empty name in SetConstant; ignore");
                    } else if (Double.TryParse(val, out value)) {
                        // The value is a number, so we're good
                        keys.Add(name, value);
                        Debug.WriteLine("Constant " + name + " defined as " + value);
                    } else {
                        double result = EvaluateExpression(item, val, KeysStack, null);
                        if (result != Double.NaN) {
                            keys.Add(name, result);
                            Debug.WriteLine("Constant " + name + ": " + val + " evaluated to " + result);
                        } else {
                            Debug.WriteLine("Constant " + name + " evaluated as NaN");
                        }
                    }
                } else if (item is ISequenceContainer descendant && descendant.Items.Count > 0) {
                    FindConstants(descendant, new Keys());
                }
            }
            
            if (cachedKeys == null) {
                if (KeyCache.ContainsKey(container)) {
                    KeyCache.TryRemove(container, out _);
                }
                if (keys.Count > 0) {
                    KeyCache.TryAdd(container, keys);
                }
            }
            
            KeysStack.Pop();

            if (keys.Count > 0) {
                Debug.WriteLine("Constants in " + container.Name + ": " + keys);
            }
        }

        public static bool IsValid(object obj, string exprName, string expr, out double val, IList<string> issues) {
            val = 0;
            ISequenceItem item = obj as ISequenceItem;
            if (item == null || item.Parent == null) return false;
            
            // Make sure we're up-to-date on constants
            ISequenceContainer root = FindRoot(item.Parent);
            Keys kk;
            if (root != null && (KeyCache.IsNullOrEmpty() || (KeyCache.Count == 1 && KeyCache.TryGetValue(GlobalContainer, out kk)))) {
                UpdateConstants(item);
            }

            if (expr == null || expr.Length == 0) {
                //Debug.WriteLine("IsValid: " + exprName + " null/empty");
                return false;
            }
            // Best case, this is a number a some sort

            if (double.TryParse(expr, out val)) {
                Debug.WriteLine("IsValid: " + item.Name + ", " + exprName + " = " + expr);
                return true;
            } else {
                ISequenceContainer c = item.Parent;
                // Ok, it's not a number. Let's look for constants
                if (c != null) {
                    // Build the keys stack
                    Stack<Keys> stack = new Stack<Keys>();
                    ISequenceContainer cc = c;
                    while (cc != null) {
                        Keys cachedKeys;
                        KeyCache.TryGetValue(cc, out cachedKeys);
                        if (!cachedKeys.IsNullOrEmpty()) {
                            stack.Push(cachedKeys);
                        }
                        if (cc == root) {
                            cc = GlobalContainer;
                        } else {
                            cc = cc.Parent;
                        }
                    }

                    // Reverse the stack to maintain proper scoping
                    Stack<Keys> reverseStack = new Stack<Keys>();
                    Keys k;
                    while (stack.TryPop(out k)) {
                        reverseStack.Push(k);
                    }

                    double result = EvaluateExpression(item, expr, reverseStack, issues);
                    Debug.WriteLine("IsValid: " + item.Name + ", " + exprName + " = " + expr + 
                        ((issues.IsNullOrEmpty()) ? (" (" + result + ")") : " issue: " + issues[0]));

                    if (Double.IsNaN(result)) {
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

        public static bool Evaluate(SequenceItem item, string exprName, string valueName, object def) {
            return Evaluate(item, exprName, valueName, def, null);
        }

        public static bool Evaluate(SequenceItem item, string exprName, string valueName, object def, IList<string> issues) {
            double val;
            string expr = item.TryGetPropertyValue(exprName, "") as string;

            PropertyInfo pi = item.GetType().GetProperty(valueName);
            if (IsValid(item, exprName, expr, out val, issues)) {
                try {
                    var conv = Convert.ChangeType(val, pi.PropertyType);
                    pi.SetValue(item, conv);
                    return true;
                } catch (Exception) {
                }
            }
            try {
                pi.SetValue(item, def);
            } catch (Exception) {
                try {
                    var conv = Convert.ChangeType(def, pi.PropertyType);
                    pi.SetValue(item, conv);
                } catch (Exception ex) {
                    Debug.WriteLine("Caught exception: " + ex);
                }
            }
            return false;
        }

        private static void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            throw new NotImplementedException();
        }
    }
}
