﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhenPlugin.When {
    internal interface ISettable {
        abstract string GetSettable();
        abstract string GetValueExpression();
        abstract void IsDuplicate(bool val);
        abstract string GetType();
    }
}
