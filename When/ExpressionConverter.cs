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

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture) {
            // value will be a string
            if (value[0] is string expr) {
                double test;
                if (double.TryParse(expr, out test)) {
                    return test;
                } else {
                    double result;
                    IList<string> issues = new List<string>();
                    SequenceItem item = value[1] as SequenceItem;
                    if (ConstantExpression.IsValidExpression(item, "foo", expr, out result, issues)) {
                        return " {" + result.ToString() + "}";
                    } else if (issues.Count > 0) {
                        return " {" + issues[0] + "}";
                    } else return " {Error}";
                 }
            }
            return "Illegal";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}