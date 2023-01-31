using Accord.Imaging.Filters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhenPlugin.When {

    public class InstructionResult: Dictionary<string, Object> {

        public Object getResult(string key) {

            if (TryGetValue(key, out Object result)) {
                return result;
            }
            return null;

        }
    }
}
