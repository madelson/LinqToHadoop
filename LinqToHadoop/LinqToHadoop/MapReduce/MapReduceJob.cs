using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToHadoop.Compiler;

namespace LinqToHadoop.MapReduce
{
    public class MapReduceJob<TMapIn, TMapKeyOut, TMapValueOut, TReduceKeyOut, TReduceValueOut>
    {
        public Func<IEnumerable<TMapIn>, IEnumerable<KeyValuePair<TMapKeyOut, TMapValueOut>>> Mapper { get; set; }
        public Func<IEnumerable<IGrouping<TMapKeyOut, TMapValueOut>>, IEnumerable<KeyValuePair<TMapKeyOut, TMapValueOut>>> Combiner { get; set; }
        public Func<IEnumerable<IGrouping<TMapKeyOut, TMapValueOut>>, IEnumerable<KeyValuePair<TReduceKeyOut, TReduceValueOut>>> Reducer { get; set; }

        public void SetMapper(Func<TMapIn, IEnumerable<KeyValuePair<TMapKeyOut, TMapValueOut>>> singleItemMapper) 
        {
            this.Mapper = input => input.SelectMany(singleItemMapper);
        }

        public void RunMapper()
        {
            Throw.If(this.Mapper == null, "Mapper required!");

            IEnumerable<TMapIn> input = null;
            foreach (var output in this.Mapper(input))
            {
                // write
            }
        }
    }
}
