using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToHadoop;
using LinqToHadoop.Reflection;
using System.Linq.Expressions;

namespace Tests.Reflection
{
    public static class ReflectionTests
    {
        public static void RunAll()
        {
            TestInference();
        }

        public static void TestInference()
        {
            var join = Helpers.Method<IQueryable<object>>(q => q.Join(q, o => 1, o => 1, (o1, o2) => 2));
            var strategy = TypeInference.InferenceStrategy.Derive(join);

            var types = new[] { typeof(IQueryable<string>), typeof(IQueryable<bool>), typeof(Expression<Func<string, char>>), typeof(Expression<Func<bool, char>>), typeof(Expression<Func<char, char, long>>) };
            var stringBoolJoin = strategy.MakeGenericMethod(types);
            stringBoolJoin.GetGenericArguments().SequenceEqual(new[] { typeof(string), typeof(bool), typeof(char), typeof(long) })
                .Assert();

            var x = types.AsQueryable()
                .Select(t => new { t.Name, z = t.Module == null });
        }
    }
}
