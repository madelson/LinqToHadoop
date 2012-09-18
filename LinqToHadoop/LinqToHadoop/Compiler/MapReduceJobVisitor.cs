using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using LinqToHadoop.Entities;

namespace LinqToHadoop.Compiler
{
    class MapReduceJobVisitor : BranchTrackingVisitor
    {
        private readonly IDictionary<string, MapReduceJob> _jobs = new Dictionary<string, MapReduceJob>();

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var m = node.Method;
            if (m.DeclaringType == typeof(Queryable) || m.DeclaringType == typeof(QueryOperators))
            {
                // first visit all children
                var parameterExpressions = this.Visit(node.Arguments);
                MapReduceJob job;

                switch (m.Name)
                {
                    case "Map":
                        var mapper = ExpressionHelpers.UnQuote(node.Arguments[1]);
                        job = new MapReduceJob(this.Path, mapExpression: (LambdaExpression)mapper);
                        break;
                    case "Reduce":
                        var reducer = ExpressionHelpers.UnQuote(node.Arguments[1]);
                        job = new MapReduceJob(this.Path, reduceExpression: (LambdaExpression)reducer);
                        break;
                    case "Combine":
                        var combiner = ExpressionHelpers.UnQuote(node.Arguments[1]);
                        job = new MapReduceJob(this.Path, combineExpression: (LambdaExpression)combiner);
                        break;
                    default:
                        var method = this.GetType().GetMethod(m.Name, BindingFlags.Instance | BindingFlags.NonPublic);
                        Throw.InvalidIf(method == null, m + " is not supported by LINQ to hadoop!");
                        var translatedExpression = (Expression)method.InferGenericsAndInvoke(parameters: node.Arguments.Cast<object>().ToArray());
                        return this.Visit(translatedExpression);
                }

                this._jobs[this.Path] = job;
                return job.OutputParameterExpression;
            }

            return base.VisitMethodCall(node);
        }

        private Expression Distinct<T>(IQueryable<T> query)
        {
            var converted = query.Map(values => values.Select(v => v.AsKey()))
                .Combine(g => new[] { g.Key.AsKey() })
                .Reduce(g => new[] { g.Key.AsValue() });
            return converted.Expression;
        }

        private Expression Select<TIn, TOut>(IQueryable<TIn> query, Expression<Func<TIn, TOut>> selector)
        {
            var converted = query.Map(values => values.Select(selector.Compile()).Select(v => v.AsValue()));
            return converted.Expression;
        }

        private Expression Where<T>(IQueryable<T> query, Expression<Func<T, bool>> predicate)
        {
            var converted = query.Map(values => values.Where(predicate.Compile()).Select(v => v.AsValue()));
            return converted.Expression;
        }

        private Expression Take<T>(IQueryable<T> query, int numToTake)
        {
            var converted = query.Map(values => values.Take(numToTake).Select(v => v.AsValue()))
                .Combine(g => g.Take(numToTake).Select(v => v.AsValue()))
                .Reduce(g => g.Take(numToTake).Select(v => v.AsValue()));
            return converted.Expression;
        }

        public class JobInfo
        {
            public MapReduceJob Job { get; private set; }
        }
    }
}
