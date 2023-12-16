using NCalc;
using NCalc.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhenPlugin.When {
    public class Expr {

        public Expr (string expr) {
            Expression = expr;
        }

        private string _expression;
        public string Expression {
            get => _expression;
            set {
                if (value == null) return;
                Double result;
                _expression = value;
                if (Double.TryParse(value, out result)) {
                    Value = result;
                } else {
                    IsExpression = true;
                    Expression e = new Expression(value, EvaluateOptions.IgnoreCase);
                    e.Parameters = new Dictionary<string, object>();
                    try {
                        e.Evaluate();
                    } catch (Exception ex) {

                    }

                    var pe = e.ParsedExpression;
                    ParameterExtractionVisitor visitor = new ParameterExtractionVisitor();
                    pe.Accept(visitor);
                    References = visitor.Parameters;
                }
            }
        }

        private double _value;
        public double Value { get; set; }

        public bool IsExpression { get; set; } = false;

        public HashSet<string> References { get; set; } = new HashSet<string>();

        class ParameterExtractionVisitor : LogicalExpressionVisitor {
            public HashSet<string> Parameters = new HashSet<string>();

            public override void Visit(NCalc.Domain.Identifier function) {
                //Parameter - add to list
                Parameters.Add(function.Name);
            }

            public override void Visit(NCalc.Domain.UnaryExpression expression) {
                expression.Expression.Accept(this);
            }

            public override void Visit(NCalc.Domain.BinaryExpression expression) {
                //Visit left and right
                expression.LeftExpression.Accept(this);
                expression.RightExpression.Accept(this);
            }

            public override void Visit(NCalc.Domain.TernaryExpression expression) {
                //Visit left, right and middle
                expression.LeftExpression.Accept(this);
                expression.RightExpression.Accept(this);
                expression.MiddleExpression.Accept(this);
            }

            public override void Visit(Function function) {
                foreach (var expression in function.Expressions) {
                    expression.Accept(this);
                }
            }

            public override void Visit(LogicalExpression expression) {

            }

            public override void Visit(ValueExpression expression) {

            }
        }

    }


}
