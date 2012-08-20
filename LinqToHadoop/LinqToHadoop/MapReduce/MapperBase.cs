using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToHadoop.MapReduce
{
    public abstract class MapperBase<TKeyIn, TValueIn, TKeyOut, TValueOut>
    {
        protected abstract IEnumerable<KeyValuePair<TKeyOut, TValueOut>> Map(IEnumerable<KeyValuePair<TKeyIn, TValueIn>> keyValuePairs);
    }
}
