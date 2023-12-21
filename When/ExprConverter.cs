#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Sequencer;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace WhenPlugin.When {

    public class ExprConverter : IMultiValueConverter {

        public static Dictionary<ISequenceEntity, bool> ValidityCache = new Dictionary<ISequenceEntity, bool>();

        public static string NOT_DEFINED = "Parameter was not defined (Parameter";

        private const int VALUE_EXPR = 0;              // The expression to be evaluated
        private const int VALUE_VALIDATE = 1;          // If present, a validation method (range check, etc.)
        private const int VALUE_TYPE = 2;              // If present, the type of result needed ("Integer" is the only value supported; others will be Double)
        private const int VALUE_COMBO = 6;             // If present, a IList<string> of combo box values

        private string Validate (ISequenceEntity item, double val, object[] values) {
            if ((values.Length > (VALUE_VALIDATE -1)) && values[VALUE_VALIDATE] is string validationMethod) {
                MethodInfo m = item.GetType().GetMethod(validationMethod);
                if (m != null) {
                    string error = (string)m.Invoke(item, new object[] { val });
                    if (error != string.Empty && item is IValidatable vitem) {
                        vitem.Issues.Add(error);
                        ValidityCache.Remove(item);
                        if (error.Equals("True")) {
                            ValidityCache.Add(item, true);
                        }
                        return " { " + error + " } ";
                    }
                }              
            }
            return string.Empty;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            string expr = values[VALUE_EXPR] as String;
            if (expr != null) {
                return "{"+ expr + "}";
 //               if (!expr.IsExpression) return expr.Value;
 //               return "{" + ((expr.Error != null) ? expr.Error : expr.Value.ToString()) + "}";
        
            } else {
                return "{" + "FuvkS" + "}";
            }
        }

 
        object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}