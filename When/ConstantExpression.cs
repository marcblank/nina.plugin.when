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
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Linq;

namespace WhenPlugin.When {
    public class ConstantExpression {
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
 
        private static Stack<Keys> KeysStack = new Stack<Keys>();

        private static int FC = 0;

        static public void UpdateConstants(ISequenceItem item) {
            KeyCache.Clear();
            FindConstants(item.Parent, new Keys());
        }
        static private void FindConstantsRoot(ISequenceContainer container, Keys keys) {
            Debug.WriteLine("Root: #" + ++FC);
            FindConstants(container, keys);
        }

        static private Double EvaluateExpression (string expr, Stack<Keys> stack, IList<string> issues) {
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
                Debug.WriteLine("Expression " + expr + " evaluated to " + eval);
                try {
                    return (double)eval;
                } catch (Exception ex) {
                    return Double.NaN;
                }
            } catch (Exception ex) {
                if (issues != null) {
                    issues.Add(ex.Message);
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
                    string val = sc.ValueExpr;
                    double value;
                    if (Double.TryParse(val, out value)) {
                        // The value is a number, so we're good
                        keys.Add(name, value);
                        Debug.WriteLine("Constant " + name + " defined as " + value);
                    } else {
                        double result = EvaluateExpression(val, KeysStack, null);
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
                        if (!cachedKeys.IsNullOrEmpty()) {
                            stack.Push(cachedKeys);
                        }
                        cc = cc.Parent;
                    }

                    // Reverse the stack to maintain proper scoping
                    Stack<Keys> reverseStack = new Stack<Keys>();
                    Keys k;
                    while (stack.TryPop(out k)) {
                        reverseStack.Push(k);
                    }

                    double result = EvaluateExpression(expr, reverseStack, issues);

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
