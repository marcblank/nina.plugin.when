using Namotion.Reflection;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using System;
using System.Collections.Generic;
using System.Data;
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
        public static bool IsValidExpression(SequenceItem item, string exprName, string expr, out double val, IList<string> issues) {
            val = 0;
 
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
                    return false;
                }
            }
            return false;
        }

    }
}
