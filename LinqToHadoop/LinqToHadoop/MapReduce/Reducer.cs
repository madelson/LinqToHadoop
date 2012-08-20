using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToHadoop.MapReduce
{
    public delegate IEnumerable<KeyValuePair<TKeyOut, TValueOut>> Reducer<TKeyIn, TValueIn, TKeyOut, TValueOut>(IEnumerable<IGrouping<TKeyIn, TValueIn>> groupings);
}
