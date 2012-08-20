using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToHadoop.Compiler;

namespace Tests.Compiler
{
    public class QueryCompilerTest
    {
        public static void RunAll()
        {
            CanonicalizeTests();
        }

        public static void CanonicalizeTests()
        {
            var q = "too bad it's 5:04PM on sunday".AsQueryable();

            var q1 = q.Where(ch => ch % 2 == 0)
                .GroupBy(ch => char.IsDigit(ch), (isDigit, chars) => isDigit ? chars.Select(ch => new string(ch, 2)).Select(int.Parse).Sum() : chars.Count());
            var exp1 = QueryCompiler.Canonicalize(q1);
            var q1c = q1.Provider.CreateQuery<int>(exp1);
            q1.SequenceEqual(q1c).Assert();

            "a".Aggregate((c1, c2) => c1);
        }
    }
}
