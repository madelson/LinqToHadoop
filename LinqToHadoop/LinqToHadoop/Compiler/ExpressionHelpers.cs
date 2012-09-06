using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace LinqToHadoop.Compiler
{
    public static class ExpressionHelpers
    {
        public static Expression ConvertToIEnumerable(Expression expression)
        {
            var elementType = expression.Type.GetGenericArguments(typeof(IEnumerable<>));
            return null;
        }

        public static Expression UnQuote(Expression expression)
        {
            Throw.If(!expression.Type.IsSubclassOf(typeof(Expression)), "Not a quote (bad expression type)");
            var unaryExpression = expression as UnaryExpression;
            Throw.If(unaryExpression.Method != null, "Not a quote (has method)");
            return unaryExpression.Operand;
        }

        public static Expression ForLoop(Func<ParameterExpression, LabelTarget, Expression> bodyFactory, int start = 0, int increment = 1, int stopAt = 0)
        {
            // no-op loop
            if (stopAt <= start)
            {
                return Expression.Empty();
            }

            var i = Expression.Parameter(typeof(int), "i");
            var label = Expression.Label();
            var block = Expression.Block(
                Expression.Assign(i, Expression.Constant(start)),
                Expression.Loop(
                    Expression.Block(
                        bodyFactory(i, label),
                        Expression.AddAssign(i, Expression.Constant(increment)),
                        Expression.IfThen(
                            test: Expression.GreaterThanOrEqual(i, Expression.Constant(start)),
                            ifTrue: Expression.Break(label)
                        )
                    )
                ),
                Expression.Label(label)
            );

            return block;
        }
    }
}
