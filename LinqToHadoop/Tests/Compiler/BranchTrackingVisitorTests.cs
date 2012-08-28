using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using LinqToHadoop.Compiler;

namespace Tests.Compiler
{
    public static class BranchTrackingVisitorTests
    {
        public static void RunAll()
        {
            var e = Enumerable.Empty<string>().AsQueryable();
            
            var exp1 = e.Where(s => s.Length > 0)
                .SelectMany(s => s)
                .GroupBy(ch => char.IsDigit(ch))
                .Take(1)
                .Map(gs => gs.Select(g => new KeyValuePair<int, int>(1, 1)))
                .Expression;
            Test(exp1, d => d.First(kvp => kvp.Key is MethodCallExpression && ((MethodCallExpression)kvp.Key).Method.Name == "GroupBy")
                .Value.ShouldEqual("0/0"));

            var exp2 = e.Join(e, a => a.Length, b => b.Length, (s1, s2) => s1 + s2)
                .Select(s => new
                {
                    Count = s.Length,
                    NumDigits = s.Count(char.IsDigit)
                })
                .Expression;
            Test(exp2);
        }

        private static void Test(Expression exp, Action<Dictionary<Expression, string>> additionalCheck = null)
        {
            var vis = new Visitor();
            vis.Visit(exp);

            var sortedPaths = vis.paths.OrderBy(s => s);
            sortedPaths.SequenceEqual(vis.paths).Assert();

            if (additionalCheck != null)
            {
                additionalCheck(vis.pathsDict);
            }
        }

        private class Visitor : BranchTrackingVisitor
        {
            public readonly List<string> paths = new List<string>();
            public readonly Dictionary<Expression, string> pathsDict = new Dictionary<Expression, string>();

            protected override Expression VisitImpl(Expression node)
            {
                this.paths.Add(this.Path);
                this.pathsDict[node] = this.Path;
                return base.VisitImpl(node);
            }
        }
    }
}
