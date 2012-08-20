using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToHadoop.MapReduce
{
    public delegate IEnumerable<KeyValuePair<TKeyOut, TValueOut>> Mapper<TKeyIn, TValueIn, TKeyOut, TValueOut>(IEnumerable<KeyValuePair<TKeyIn, TValueIn>> keyValuePairs);
}
