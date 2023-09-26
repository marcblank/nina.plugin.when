using Accord;
using Castle.Core.Internal;
using Namotion.Reflection;
using NCalc;
using NCalc.Domain;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Profile;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using Nito.Mvvm;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace WhenPlugin.When {
    public class ConstantExpression {
        static SequenceRootContainer FindRoot(ISequenceEntity cont) {
            while (cont != null) {
                if (cont is SequenceRootContainer root) { return root; }
                if (cont is IfContainer ifc) {
                    cont = ifc.PseudoParent;
                } else {
                    cont = cont.Parent;
                }
            }
            return null;
        }

        static private ConcurrentDictionary<ISequenceEntity, Keys> KeyCache = new ConcurrentDictionary<ISequenceEntity, Keys>();

        static private bool Debugging = false;

        public class Keys : Dictionary<string, object> {

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

        static public void FlushContainerKeys(ISequenceContainer container) {
            KeyCache.TryRemove(container, out _);
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

        private static readonly object ConstantsLock = new object();

        static public void UpdateConstants(ISequenceItem item) {
            lock (ConstantsLock) {
                ISequenceContainer root = GetRoot(item);
                if (root != null) {
                    KeyCache.Clear();
                    FindConstantsRoot(root, new Keys());
                    if (Debugging) {
                        DebugInfo("**KeyCache: ", KeyCache.Count.ToString(), " **");
                        foreach (var kvp in KeyCache) {
                            DebugInfo(kvp.Key.Name, ": ", kvp.Value.ToString());
                            //foreach (var c in kvp.Value) {
                            //    DebugInfo(c.ToString());
                            //}
                        }
                    }
                } else if (item.Parent != null) {
                    FindConstants(GlobalContainer, new Keys());
                }
            }
        }

        static public SequenceContainer GlobalContainer = new SequentialContainer() { Name = "Global Constants" };

        static private bool InFlight { get; set; } = false;

        static private void FindConstantsRoot(ISequenceContainer container, Keys keys) {
            // We start from root, but we'll add global constants
            DebugInfo("Root: #", (++FC).ToString());
            if (!GlobalContainer.Items.Contains(container)) {
                GlobalContainer.Items.Add(container);
            }
            FindConstants(GlobalContainer, keys);
        }

        static private Double EvaluateExpression(ISequenceItem item, string expr, Stack<Keys> stack, IList<string> issues) {
            if (expr.IsNullOrEmpty()) return 0;

            if (string.Equals(expr, "true", StringComparison.OrdinalIgnoreCase)) { return 1; }
            if (string.Equals(expr, "false", StringComparison.OrdinalIgnoreCase)) { return 0; }

            Expression e = new Expression(expr, EvaluateOptions.IgnoreCase);
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
                DebugInfo("Expression '", expr, "' not evaluated; no keys");
                return Double.NaN;
            }
            e.Parameters = mergedKeys;
            try {
                var eval = e.Evaluate();
                DebugInfo("Expression '", expr, "' in '", item.Name , "' evaluated to " + eval.ToString());
                if (eval is Boolean b) {
                    return b ? 1 : 0;
                }
                try {
                    return Convert.ToDouble(eval);
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

        static Keys GetMergedKeys(Stack<Keys> stack) {
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
            return mergedKeys;
        }

        static Keys GetParsedKeys(LogicalExpression e, Keys mergedKeys, Keys k) {
            if (e is BinaryExpression b) {
                GetParsedKeys(b.LeftExpression, mergedKeys, k);
                GetParsedKeys(b.RightExpression, mergedKeys, k);
            } else if (e is Identifier i) {
                k.Add(i.Name, mergedKeys.GetValueOrDefault(i.Name));
            } else if (e is TernaryExpression t) {
                Logger.Info("LI");
            } else if (e is Function f) {
                if (f.Expressions != null) {
                    foreach (LogicalExpression ee in  f.Expressions) {
                        GetParsedKeys(ee, mergedKeys, k);
                    }
                }
            }
            return k;
        }

       static string FindKey(ISequenceEntity item, string key) {
            ISequenceContainer root = FindRoot(item.Parent);
            ISequenceEntity p = item.Parent;
            while (p != null) {
                Keys k = KeyCache.GetValueOrDefault(p, null);
                if (k != null) {
                    if (k.ContainsKey(key)) {
                        return (p == item.Parent ? "Here" : p == GlobalContainer ? "Global" : p.Name);
                    }
                }
                if (p == root) {
                    p = GlobalContainer;
                } else if (p is IfContainer ifc) {
                    p = ifc.PseudoParent;
                } else {
                    p = p.Parent;
                }
            }
            return "??";
        }

        static public string DissectExpression(ISequenceItem item, string expr, Stack<Keys> stack) {
            if (expr.IsNullOrEmpty()) return String.Empty;

            Expression e = new Expression(expr, EvaluateOptions.IgnoreCase);
            // Consolidate keys
            Keys mergedKeys = GetMergedKeys(stack);
            if (mergedKeys.Count == 0) {
                DebugInfo("Expression ", expr, " not evaluated; no keys");
                return String.Empty;
            }
            e.Parameters = mergedKeys;
            try {
                var eval = e.Evaluate();
                // Find the keys used in the expression
                Keys parsedKeys = GetParsedKeys(e.ParsedExpression, mergedKeys, new Keys());
                StringBuilder stringBuilder = new StringBuilder("Constants used: ");
                int cnt = parsedKeys.Count;
                if (cnt == 0) {
                    stringBuilder.Append("None");
                } else {
                    foreach (var key in parsedKeys) {
                        string whereDefined = FindKey(item, key.Key);
                        stringBuilder.Append(key.Key + " (" + whereDefined + ") = " + key.Value);
                        if (--cnt > 0) stringBuilder.Append("; ");
                    }
                }
                return (stringBuilder.ToString());
            } catch (Exception ex) {
                if (ex is EvaluationException) {
                    return ("Syntax error");
                } else {
                    return ("Error: " + ex.Message);
                }
            }
        }

        static public Stack<Keys> GetKeyStack(ISequenceEntity item) {

            // Build the keys stack, walking up the ladder of Parents
            ISequenceContainer root = FindRoot(item.Parent);
            Stack<Keys> stack = new Stack<Keys>();
            ISequenceEntity cc = item.Parent;
            while (cc != null) {
                Keys cachedKeys;
                KeyCache.TryGetValue(cc, out cachedKeys);
                if (!cachedKeys.IsNullOrEmpty()) {
                    stack.Push(cachedKeys);
                }
                if (cc == root) {
                    cc = GlobalContainer;
               } else {
                    cc = GetParent(cc);
                }
            }

            // Reverse the stack to maintain proper scoping
            Stack<Keys> reverseStack = new Stack<Keys>();
            Keys k;
            while (stack.TryPop(out k)) {
                reverseStack.Push(k);

            }
            return reverseStack;
        }


        static ISequenceEntity GetParent(ISequenceEntity p) {
            if (p is IfContainer ic) {
                return (ic.Parent == null ? ic.PseudoParent : ic.Parent);
            } else {
                return p.Parent;
            }

        }

        static private bool IsAttachedToRoot(ISequenceContainer container) {
            ISequenceEntity p = container;
            while (p != null) {
                if (p is SequenceRootContainer) {
                    return true;
                } else {
                    p = GetParent(p);
                }
            }
            return false;
        }

        static private void FindConstants(ISequenceContainer container, Keys keys) {

            if (!Loaded && (container != GlobalContainer)) return;

            if (container == null) return;
            if (container.Items.IsNullOrEmpty()) return;

            Keys cachedKeys = null;
            if (KeyCache.TryGetValue(container, out cachedKeys)) {
                DebugInfo("FindConstants for '", container.Name, "' found in cache: ", cachedKeys.ToString());
            } else {
                DebugInfo("FindConstants for '", container.Name, "'");
            }

            KeysStack.Push(keys);

            foreach (ISequenceItem item in container.Items) {
                if (item is SetConstant sc && cachedKeys == null) {
                    string name = sc.Constant;
                    string val = sc.CValueExpr;
                    double value;
                    sc.DuplicateName = false;
                    if (item.Parent != container) {
                        // In this case item has been deleted from parent (but it's still in Parent's Items)
                    } else if (name.IsNullOrEmpty()) {
                        DebugInfo("Empty name in SetConstant; ignore");
                    } else if (Double.TryParse(val, out value)) {
                        // The value is a number, so we're good
                        try {
                            keys.Add(name, value);
                        } catch (Exception) {
                            // Multiply defined...
                            sc.DuplicateName = true;
                        }
                        DebugInfo("Constant '", name, "' defined as ", value.ToString());
                    } else {
                        double result = EvaluateExpression(item, val, KeysStack, null);
                        if (!double.IsNaN(result)) {
                            try {
                                keys.Add(name, result);
                            } catch (Exception) {
                                // Multiply defined...
                                sc.DuplicateName = true;
                            }
                            DebugInfo("Constant '", name, "': ", val, " evaluated to ", result.ToString());
                        } else {
                            DebugInfo("Constant '", name, "' evaluated as NaN");
                        }
                    }
                } else if (item is IfCommand ifc && ifc.Instructions.Items.Count > 0) {
                    FindConstants(ifc.Instructions, new Keys());
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
                DebugInfo("Constants defined in '", container.Name, "': ", keys.ToString());
            }
        }

        private static bool Loaded { get; set; } = false;

        public static bool IsValid(object obj, string exprName, string expr, out double val, IList<string> issues) {
            val = 0;
            ISequenceItem item = obj as ISequenceItem;
            if (item == null || item.Parent == null) return false;

            if (expr == null || expr.Length == 0) {
                DebugInfo("IsValid: ", exprName, " null/empty");
                return false;
            }

            // We will always process the Global container
            if (item.Parent != GlobalContainer) {
                if (!IsAttachedToRoot(item.Parent)) return true;
                // Say that we have a sequence loaded...
                Loaded = true;
            }

            // Make sure we're up-to-date on constants
            ISequenceContainer parent = item.Parent;
            ISequenceContainer root = FindRoot(parent);
            Keys kk;
            if (root != null && (KeyCache.IsNullOrEmpty() || (KeyCache.Count == 1 && KeyCache.TryGetValue(GlobalContainer, out kk)))) {
                UpdateConstants(item);
            } else if (!(parent is IImmutableContainer) && !KeyCache.ContainsKey(parent)) {
                // The IImmutableContainer case is for TakeManyExposures and SmartExposure, which are containers and items
                UpdateConstants(item);
            }

            // Best case, this is a number a some sort
            if (double.TryParse(expr, out val)) {
                DebugInfo("IsValid for ", item.Name, ": '", exprName, "' = ", expr);
                return true;
            } else {
                ISequenceContainer c = item.Parent;
                // Ok, it's not a number. Let's look for constants
                if (c != null) {
                    // Build the keys stack, walking up the ladder of Parents
                    Stack<Keys> stack = new Stack<Keys>();
                    ISequenceEntity cc = c;
                    while (cc != null) {
                        Keys cachedKeys;
                        KeyCache.TryGetValue(cc, out cachedKeys);
                        if (!cachedKeys.IsNullOrEmpty()) {
                            stack.Push(cachedKeys);
                        }
                        if (cc is SequenceRootContainer) {
                            cc = GlobalContainer;
                        } else {
                            cc = GetParent(cc);
                        }
                    }

                    // Reverse the stack to maintain proper scoping
                    Stack<Keys> reverseStack = new Stack<Keys>();
                    Keys k;
                    while (stack.TryPop(out k)) {
                        reverseStack.Push(k);
                    }

                    if (reverseStack.IsNullOrEmpty() && issues != null) issues.Add("There are no valid constants defined.");

                    double result = EvaluateExpression(item, expr, reverseStack, issues);
                    DebugInfo("IsValid: ", item.Name, ", ", exprName, " = ", expr, 
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

        public static bool Evaluate(ISequenceItem item, string exprName, string valueName, object def) {
            return Evaluate(item, exprName, valueName, def, null);
        }

        public static bool Evaluate(ISequenceItem item, string exprName, string valueName, object def, IList<string> issues) {
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
                    DebugInfo("Caught exception: ", ex.Message);
                    Logger.Info("Caught exception: " + ex);
                }
            }
            return false;
        }

        private static void DebugInfo(params string[] strs) {
            if (Debugging) {
                Debug.WriteLine(String.Join("", strs));
            }
        }

        private static void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            throw new NotImplementedException();
        }
    }
}
