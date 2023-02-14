#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.SequenceItem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using static System.Net.Mime.MediaTypeNames;

namespace WhenPlugin.When {

    public class ExpressionConverter : IMultiValueConverter {

        public static Dictionary<ISequenceItem, bool> ValidityCache = new Dictionary<ISequenceItem, bool>();

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture) {
            // value will be a string
            SequenceItem item = value[1] as SequenceItem;
                if (value[0] is string expr) {
                double test;
                if (double.TryParse(expr, out test)) {
                    ValidityCache.Remove(item);
                    ValidityCache.Add(item, true);
                    return test;
                } else {
                    double result;
                    IList<string> issues = new List<string>();
                    if (ConstantExpression.IsValidExpression(item, "foo", expr, out result, issues)) {
                        ValidityCache.Remove(item);
                        ValidityCache.Add(item, true);
                        return " {" + result.ToString() + "}";
                    } else if (issues.Count > 0) {
                        ValidityCache.Remove(item);
                        string errorString = issues[0];
                        int pos = errorString.IndexOf("Parameter was not defined (");
                        if (pos == 0) {
                            errorString = "Undefined (" + errorString.Substring("Paremeter was not defined (".Length);
                        }
                        return " {" + errorString + "}";
                    } else {
                        ValidityCache.Remove(item);
                        return " {Error}";
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