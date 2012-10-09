using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToHadoop.MapReduce
{
    public interface IMapJob<TKeyIn, TValueIn, TKeyOut, TValueOut>
    {
        IEnumerable<KeyValuePair<TKeyOut, TValueOut>> Map(IEnumerable<KeyValuePair<TKeyIn, TValueIn>> items);
    }

    public interface ICombineJob<TKey, TValue>
    {
        IEnumerable<KeyValuePair<TKey, TValue>> Combine(IEnumerable<IGrouping<TKey, TValue>> groups);
    }

    public interface IReduceJob<TKeyIn, TValueIn, TKeyOut, TValueOut>
    {
        IEnumerable<KeyValuePair<TKeyOut, TValueOut>> Reduce(IEnumerable<IGrouping<TKeyIn, TValueIn>> groups);
    }
}
