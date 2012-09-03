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
            var query2 = query.Provider.CreateQuery<T>(inlined);
            var result2 = query2.ToList();

            result2.SequenceEqual(result1).Assert();
        }
    }
}
