using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Sequencer.Logic;

namespace PowerupsLite.When
{
    public class ExpressionHelper {

        public static Expression Expr (string definition, ISequenceContainer parent, ISymbolBrokerVM broker, int? def) {
            Expression e = new Expression(definition, parent);
            if (def != null) {
                e.Default = (int)def;
            }
            e.SymbolBroker = broker;
            e.Evaluate();
            return e;
        }
    }
}
