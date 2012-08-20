using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToHadoop.Compiler
{
    public static class QueryCompiler
    {
        /*
         * (1) Canonicalize expression tree, detecting unsupported query operators and replacing others with 
         * combinations of the supported ones (e. g. overloads of group by, average (translate to aggregate))
         * (2) Translate canonicalized tree to a DAG of lower level operations:
         * - Map
         * - Combine
         * - Reduce
         * (3) Compile DAG 
         */
        public static readonly MethodInfo Select = Helpers.Method<IQueryable<int>>(q => q.Select(x => x))
                .GetGenericMethodDefinition(),
            SelectMany = Helpers.Method<IQueryable<int>>(q => q.SelectMany(x => string.Empty))
                .GetGenericMethodDefinition(),
            GroupBy = Helpers.Method<IQueryable<int>>(q => q.GroupBy(x => x))
                .GetGenericMethodDefinition();

        internal static Expression Canonicalize(IQueryable queryable)
        {
            var visitor = new CompilerVisitor();
            var result = visitor.Visit(queryable.Expression);
            return result;
        }

        private class BranchTrackingVisitor : ExpressionVisitor
        {
            public const string PathSeparator = ".";
            private readonly IDictionary<int, int> _claimedBranchIdsByDepth = new Dictionary<int, int>();
            private readonly Stack<string> _pathStack = new Stack<string>();
            
            public int Depth { get; private set; }
            public string CurrentPath { get { return this._pathStack.Peek(); } }

            public BranchTrackingVisitor()
            {
                this.Depth = -1;
            }

            public override Expression Visit(Expression node)
            {
                this.Depth++;

                // claim an id for the current path
                var id = this._claimedBranchIdsByDepth.ContainsKey(this.Depth)
                    ? this._claimedBranchIdsByDepth[this.Depth] + 1
                    : 0;
                this._claimedBranchIdsByDepth[this.Depth] = id;

                // derive a path
                var path = this._pathStack.Count > 0
                    ? this._pathStack.Peek() + "." + id
                    : id.ToString();
                this._pathStack.Push(path);

                var result = base.Visit(node);

                this._pathStack.Pop();
                this.Depth--;

                return result;
            }
        }

        private class CompilerVisitor : BranchTrackingVisitor
        {
            private readonly Dictionary<string, List<MapReduceStep>> _stepsByPath = new Dictionary<string,List<MapReduceStep>>();

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var m = node.Method;
                if (m.DeclaringType == typeof(Queryable)
                    && m.IsGenericMethod)
                {
                    Console.Out.WriteLine(this.CurrentPath);
                    var args = m.GetParameters();
                    switch (m.Name)
                    {
                        case "Select":
                            break;
                    }
                }

                return base.VisitMethodCall(node);
            }
        }

        public class MapReduceStep
        {
            public enum StepType 
            {
                Map,
                Combine,
                Reduce,
            }

            public MapReduceStep(LambdaExpression Expression, StepType type)
            {
                this.Expression = Expression;
                this.Type = type;

                Throw.If(this.InputType.GetGenericTypeDefinition() != typeof(KeyValuePair<,>), this.InputType.Name + " is not a key value pair");
                Throw.If(this.OutputType.GetGenericTypeDefinition() != typeof(KeyValuePair<,>), this.OutputType.Name + " is not a key value pair");
            }

            public LambdaExpression Expression { get; private set; }
            public StepType Type { get; private set; }
            public Type InputType
            {
                get { return this.Expression.Parameters.Single().Type; }
            }
            public Type OutputType
            {
                get { return this.Expression.Body.Type; }
            }
            public Type InputKeyType
            {
                get { return this.InputType.GetGenericArguments()[0]; }
            }
            public Type InputValueType { get { return this.InputType.GetGenericArguments()[1]; } }
            public Type OutputKeyType { get { return this.OutputType.GetGenericArguments()[0]; } }
            public Type OutputValueType { get { return this.OutputType.GetGenericArguments()[1]; } }
        }

        private class CanonicalizeVisitor : ExpressionVisitor
        {
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var m = node.Method;
                if (m.DeclaringType == typeof(Queryable)
                    && m.IsGenericMethod)
                {
                    var args = m.GetParameters();
                    switch (m.Name)
                    {
                        // note that the index-based versions are allowed, although they force a map-reduce with a single
                        // key and thus aren't recommended in most scenarios
                        case "Select":
                        case "Where":
                        case "ElementAt":
                        case "Skip":
                        case "Take":
                        case "Aggregate":
                        // group join is actually fairly directly expressable in terms of map-reduce (map both sequences with a key selector,
                        // then reduce with the result selector after differentiating between the two)
                        case "GroupJoin":
                            break;
                        case "SelectMany":
                            // the two-argument version can be canonicalized into the one-argument version by applying an internal Select:
                            // q.SelectMany(x => many<T>, (x, T) => V) => q.SelectMany(x => many<T>.Select(t => v(x, t)))
                            if (args.Length > 2)
                            {
                            }
                            break;
                        case "Distinct":
                            // distinct can be expressed as groupby:
                            // q.Distinct() => q.GroupBy(q => q).Select(g => g.First())
                            //var queryableElementType = node.Arguments[0].Type.GetGenericArguments()[0];
                            //var parameter = Expression.Parameter(queryableElementType);
                            //var groupAndSelectFirst = Expression.Call(
                            //    Select.MakeGenericMethod(typeof(IGrouping<queryableElementType),
                            //    Expression.Quote(Expression.Lambda(parameter, parameter)),
                            //    Expression.Call(
                            //        GroupBy.MakeGenericMethod(queryableElementType)
                            //    )
                            //)
                            //break;
                        case "GroupBy":
                        default:
                            throw Throw.ShouldNeverGetHere("not supported");
                    }
                    var genericMethod = node.Method.GetGenericMethodDefinition();
                    var queryable = node.Arguments[0];          
                }
                return base.VisitMethodCall(node);
            }
        }
    }
}
