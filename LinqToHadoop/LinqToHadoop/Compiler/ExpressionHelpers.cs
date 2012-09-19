using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;

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

        public static Expression ForEachLoop(Expression enumerable, Func<ParameterExpression, ParameterExpression, LabelTarget, Expression> bodyFactory)
        {
            var elementType = enumerable.Type.GetGenericArguments(typeof(IEnumerable<>)).SingleOrDefault();
            Throw.If(elementType == null, enumerable + " does not implement IEnumerable<T>!");

            var index = Expression.Parameter(typeof(int), "index");
            var element = Expression.Parameter(elementType, "element");
            var iterator = Expression.Parameter(typeof(IEnumerator<>).MakeGenericType(elementType), "iterator");

            var label = Expression.Label("endOfLoop");
            var block = Expression.Block(
                new[] { index, element, iterator },
                Expression.TryFinally(
                    // try { iter = enumerable.GetEnumerator(); loop }
                    body: Expression.Block(
                        Expression.Assign(
                            iterator,
                            // we can't use the Call extension here because this isn't a generic method, it's a method on a generic type
                            Expression.Call(Expression.Convert(enumerable, typeof(IEnumerable<>).MakeGenericType(elementType)), "GetEnumerator", Type.EmptyTypes)
                        ),
                        Expression.Loop(
                            // if (iter.MoveNext()) { body } else { break }
                            Expression.IfThenElse(
                                test: Helpers.Method<IEnumerator>(e => e.MoveNext()).Call(iterator),
                                ifTrue: Expression.Block(
                                    Expression.Assign(element, Expression.MakeMemberAccess(iterator, iterator.Type.GetProperty("Current"))),
                                    bodyFactory(element, index, label),
                                    Expression.AddAssign(index, Expression.Constant(1))
                                ),
                                ifFalse: Expression.Break(label)
                            )
                        ),
                        Expression.Label(label)
                    ),
                    // finally { if (iter != null) ((IDisposable)iter).Dispose() }
                    @finally: Expression.IfThen(
                        test: Expression.NotEqual(iterator, Expression.Constant(null, iterator.Type)),
                        ifTrue: Helpers.Method<IDisposable>(d => d.Dispose()).Call(
                            Expression.Convert(iterator, typeof(IDisposable))
                        )
                    )
                )
            );

            return block;
        }

        public static Expression ForLoop(Func<ParameterExpression, LabelTarget, Expression> bodyFactory, Expression startAt = null, Expression incrementBy = null, Expression stopAt = null)
        {
            var startExpression = startAt ?? Expression.Constant(0);
            var incrementExpression = incrementBy ?? Expression.Constant(1);
            var stopAtExpression = stopAt ?? Expression.Constant(0);
            Throw.If(startExpression.Type != incrementExpression.Type, "Type mismatch between i = X and i += Y");
            Throw.If(startExpression.Type != stopAtExpression.Type, "Type mismatch between i = X and i <= Y");

            var i = Expression.Parameter(typeof(int), "i");
            var label = Expression.Label();
            var block = Expression.Block(
                new[] { i },
                Expression.Assign(i, startExpression),
                Expression.Loop(
                    Expression.Block(
                        bodyFactory(i, label),
                        Expression.AddAssign(i, incrementExpression),
                        Expression.IfThen(
                            test: Expression.GreaterThanOrEqual(i, stopAtExpression),
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
