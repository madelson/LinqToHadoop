using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

using LinqToHadoop;
using LinqToHadoop.Compiler;

namespace Tests.Compiler
{
    public static class CompilerTests
    {
        public static void RunAll()
        {
            BranchTrackingVisitorTests.RunAll();
            QueryCompilerTest.RunAll();
            TestCompilerInliner();
            TestExpressionHelpers();
        }

        public static void TestCompilerInliner()
        {
            Expression<Func<char, bool>> toInline = ch => char.IsUpper(ch);
            var e1 = "aBcD".AsQueryable().Map(chars => chars.Select(ch => ch.WithValue(toInline.Compile()(ch))))
                .Select(kvp => new { kvp.Key, kvp.Value });
            TestCompilerInliner(e1);

            Expression<Func<bool, int>> toInline2 = b => b ? 100 : 200;
            var e2 = "aBcD".AsQueryable().SelectMany(ch => new[] { ch, char.ToUpper(ch) }.Select(x => toInline2.Compile()(toInline.Compile()(x))));
            TestCompilerInliner(e2);
        }

        private static void TestCompilerInliner<T>(IQueryable<T> query)
        {
            var result1 = query.ToList();
            var inlined = CompileInliner.ProcessExpression(query.Expression);
            (!Equals(inlined, query.Expression)).Assert();
            new CompileFinder().Visit(inlined);

            var query2 = query.Provider.CreateQuery<T>(inlined);
            var result2 = query2.ToList();

            result2.SequenceEqual(result1).Assert();
        }

        public static void TestExpressionHelpers()
        {
            TestForLoop();
            TestForEachLoop();
        }

        private static void TestForLoop()
        {
            var list = new List<int>();
            Expression<Action<int>> add = i => list.Add(i);

            var forLoop = ExpressionHelpers.ForLoop(
                bodyFactory: (i, label) => Expression.Invoke(add, i),
                startAt: Expression.Constant(3),
                incrementBy: Expression.Constant(2),
                stopAt: Expression.Constant(11)
            );

            var runForLoop = Expression.Lambda<Action>(forLoop);
            var runForLoopAction = runForLoop.Compile();
            
            runForLoopAction();
            list.SequenceEqual(new[] { 3, 5, 7, 9 }).Assert();

            runForLoopAction();
            list.SequenceEqual(new[] { 3, 5, 7, 9, 3, 5, 7, 9 }).Assert();
        }

        private static void TestForEachLoop()
        {
            var list = new List<string> { "a", "bb", "", "cab" };
            var outList = new List<int>();
            Expression<Action<string, int>> addLengthAndIndex = (s, i) => outList.AddRange(new[] { s.Length, i });

            var forEachLoop = ExpressionHelpers.ForEachLoop(
                enumerable: Expression.Constant(list),
                bodyFactory: (element, index, label) => Expression.Invoke(addLengthAndIndex, element, index)
            );
            var runLoop = Expression.Lambda<Action>(forEachLoop);
            var runLoopAction = runLoop.Compile();

            runLoopAction();
            outList.SequenceEqual(new[] { 1, 0, 2, 1, 0, 2, 3, 3 }).Assert();
        }

        private class CompileFinder : ExpressionVisitor
        {
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                Throw.If(node.Method.Name == "Compile" && node.Method.DeclaringType.Is(typeof(LambdaExpression)), "Found Compile call!");

                return base.VisitMethodCall(node);
            }
        }
    }
}
