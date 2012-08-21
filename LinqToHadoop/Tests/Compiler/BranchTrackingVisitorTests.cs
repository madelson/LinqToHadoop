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
                .Expression;
            Test(exp1, null);
        }

        private static void Test(Expression exp, Action<Dictionary<Expression, string>> additionalCheck)
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
