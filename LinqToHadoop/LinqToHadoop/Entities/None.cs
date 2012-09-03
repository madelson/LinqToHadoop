using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToHadoop.Entities
{
    public class None
    {
        public static readonly None Instance = new None();

        private None()
        {
        }
    }

    public static class NoneHelpers
    {
        public static KeyValuePair<TKey, None> AsKey<TKey>(this TKey @this)
        {
            return @this.WithValue(None.Instance);
        }

        public static KeyValuePair<None, TValue> AsValue<TValue>(this TValue @this)
        {
            return @this.WithKey(None.Instance);
        }
    }
}
