using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using LinqToHadoop.Query;

namespace LinqToHadoop.Compiler
{
    public static class QueryOperators
    {
        // should this return the IEnumerable variant?
        public static MethodInfo Method(Expression<Action<IQueryable<string>>> expression)
        {
            return Helpers.Method(expression);
        }

        #region ---- Public MapReduce IQueryable Methods ----
        public static IQueryable<KeyValuePair<TKeyOut, TValueOut>> Map<TIn, TKeyOut, TValueOut>(
            this IQueryable<TIn> @this, 
            Expression<Func<IEnumerable<TIn>, IEnumerable<KeyValuePair<TKeyOut, TValueOut>>>> mapper)
        {
            if (@this is HadoopQueryable<TIn>)
            {
                return @this.MapInternal(mapper);
            }

            var result = @this.GroupBy(t => 1)
                .SelectMany(mapper);
            return result;
        }

        public static IQueryable<KeyValuePair<TKeyOut, TValueOut>> Reduce<TKeyIn, TValueIn, TKeyOut, TValueOut>(
            this IQueryable<KeyValuePair<TKeyIn, TValueIn>> @this,
            Expression<Func<IGrouping<TKeyIn, TValueIn>, IEnumerable<KeyValuePair<TKeyOut, TValueOut>>>> reducer)
        {
            if (@this is HadoopQueryable<KeyValuePair<TKeyIn, TValueIn>>)
            {
                return @this.ReduceInternal(reducer);
            }

            var result = @this.GroupBy(kvp => kvp.Key, kvp => kvp.Value)
                .SelectMany(reducer);
            return result;
        }

        public static IQueryable<KeyValuePair<TKey, TValue>> Combine<TKey, TValue>(
            this IQueryable<KeyValuePair<TKey, TValue>> @this,
            Expression<Func<IGrouping<TKey, TValue>, IEnumerable<KeyValuePair<TKey, TValue>>>> combiner)
        {
            if (@this is HadoopQueryable<KeyValuePair<TKey, TValue>>)
            {
                return @this.CombineInternal(combiner);
            }

            var result = @this.GroupBy(kvp => kvp.Key, kvp => kvp.Value)
                .SelectMany(combiner);
            return result;
        }
        #endregion

        #region ---- Internal MapReduce IQueryable Methods ----
        internal static IQueryable<KeyValuePair<TKeyOut, TValueOut>> MapInternal<TIn, TKeyOut, TValueOut>(
            this IQueryable<TIn> @this,
            Expression<Func<IEnumerable<TIn>, IEnumerable<KeyValuePair<TKeyOut, TValueOut>>>> mapper)
        {
            // add expressions
            var resultQuery = @this.Provider.CreateQuery<KeyValuePair<TKeyOut, TValueOut>>(
                ((MethodInfo)MethodBase.GetCurrentMethod()).Call(
                    @this.Expression,
                    Expression.Quote(mapper)
                )
            );
            return resultQuery;
        }

        internal static IQueryable<KeyValuePair<TKeyOut, TValueOut>> ReduceInternal<TKeyIn, TValueIn, TKeyOut, TValueOut>(
            this IQueryable<KeyValuePair<TKeyIn, TValueIn>> @this,
            Expression<Func<IGrouping<TKeyIn, TValueIn>, IEnumerable<KeyValuePair<TKeyOut, TValueOut>>>> reducer)
        {
            // add expressions
            var resultQuery = @this.Provider.CreateQuery<KeyValuePair<TKeyOut, TValueOut>>(
                ((MethodInfo)MethodBase.GetCurrentMethod()).Call(
                    @this.Expression,
                    Expression.Quote(reducer)
                )
            );
            return resultQuery;
        }

        internal static IQueryable<KeyValuePair<TKey, TValue>> CombineInternal<TKey, TValue>(
            this IQueryable<KeyValuePair<TKey, TValue>> @this,
            Expression<Func<IGrouping<TKey, TValue>, IEnumerable<KeyValuePair<TKey, TValue>>>> combiner)
        {
            // add expressions
            var resultQuery = @this.Provider.CreateQuery<KeyValuePair<TKey, TValue>>(
                ((MethodInfo)MethodBase.GetCurrentMethod()).Call(
                    @this.Expression,
                    Expression.Quote(combiner)
                )
            );
            return resultQuery;
        }
        #endregion
    }
}
