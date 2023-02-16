#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Diagnostics;
using Castle.Core.Internal;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Markup;
using static System.Net.Mime.MediaTypeNames;

namespace WhenPlugin.When {

    public class ExpressionConverter : IMultiValueConverter {

        public static Dictionary<ISequenceItem, bool> ValidityCache = new Dictionary<ISequenceItem, bool>();

        public static string Not_Defined = "Parameter was not defined (";

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture) {
            // value will be a string
            SequenceItem item = value[1] as SequenceItem;
          if (value[0] is string expr) {
                double val;
                if (expr.IsNullOrEmpty() && parameter != null && parameter.GetType() == typeof(String) && parameter.Equals("Hint")) {
                    ValidityCache.Remove(item);
                    ValidityCache.Add(item, true);
                    return 0;
                } else if (double.TryParse(expr, out val)) {
                    if (value.Length > 3 && value[3] is string validationMethod) {
                        MethodInfo m = item.GetType().GetMethod(validationMethod);
                        if (m != null) {
                            string error = (string)m.Invoke(item, new object[] { val });
                            if (error != string.Empty && item is IValidatable vitem) {
                                vitem.Issues.Add(error);
                                ValidityCache.Remove(item) ;
                                return " { " + error + "} ";
                            }
                        }
                    }
                    ValidityCache.Remove(item);
                    ValidityCache.Add(item, true);
                    return val;
                } else {
                    double result;
                    IList<string> issues = new List<string>();
                    if (ConstantExpression.IsValid(item, "*Converter*", expr, out result, issues)) {
                        ValidityCache.Remove(item);
                        ValidityCache.Add(item, true);
                        return " {" + result.ToString() + "}";
                    } else if (issues.Count > 0) {
                        ValidityCache.Remove(item);
                        string errorString = issues[0];
                        int pos = errorString.IndexOf(Not_Defined);
                        if (pos == 0) {
                            errorString = "Undefined (" + errorString.Substring(Not_Defined.Length);
                        }
                        return " {" + errorString + "} ";
                    } else {
                        ValidityCache.Remove(item);
                        return " {Error} ";
                    }
                 }
            }
            ValidityCache.Remove(item);
            return "Illegal";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}