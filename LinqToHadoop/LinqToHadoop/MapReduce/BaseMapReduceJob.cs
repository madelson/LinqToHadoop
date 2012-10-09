using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToHadoop.IO;
using System.Linq.Expressions;

namespace LinqToHadoop.MapReduce
{
    public abstract class BaseMapReduceJob<TKeyIn, TValueIn, TKeyIntermediate, TValueIntermediate, TKeyOut, TValueOut>
        : IMapJob<TKeyIn, TValueIn, TKeyIntermediate, TValueIntermediate>,
          ICombineJob<TKeyIntermediate, TValueIntermediate>,
          IReduceJob<TKeyIntermediate, TValueIntermediate, TKeyOut, TValueOut>
    {
        #region ---- Core operations ----
        public virtual IEnumerable<KeyValuePair<TKeyIntermediate, TValueIntermediate>> Map(IEnumerable<KeyValuePair<TKeyIn, TValueIn>> items)
        {
            return items.SelectMany(this.MapSingle);
        }

        public virtual IEnumerable<KeyValuePair<TKeyIntermediate, TValueIntermediate>> Combine(IEnumerable<IGrouping<TKeyIntermediate, TValueIntermediate>> groups)
        {
            return groups.SelectMany(this.CombineSingle);
        }

        public virtual IEnumerable<KeyValuePair<TKeyOut, TValueOut>> Reduce(IEnumerable<IGrouping<TKeyIntermediate, TValueIntermediate>> groups)
        {
            return groups.SelectMany(this.ReduceSingle);
        }
        #endregion

        #region ---- Single element operations ----
        public virtual IEnumerable<KeyValuePair<TKeyIntermediate, TValueIntermediate>> MapSingle(KeyValuePair<TKeyIn, TValueIn> pair)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<KeyValuePair<TKeyIntermediate, TValueIntermediate>> CombineSingle(IGrouping<TKeyIntermediate, TValueIntermediate> group)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<KeyValuePair<TKeyOut, TValueOut>> ReduceSingle(IGrouping<TKeyIntermediate, TValueIntermediate> group)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region ---- Job runners ----
        public void RunMapJob(IReader reader, IWriter writer)
        {
            this.CheckImplementsOne(j => j.Map(null), j => j.MapSingle(default(KeyValuePair<TKeyIn, TValueIn>)));
            var readerFunc = Deserializer<KeyValuePair<TKeyIn, TValueIn>>.ReadExpression.Compile();
            var writerFunc = Serializer<KeyValuePair<TKeyIntermediate, TValueIntermediate>>.WriteExpression.Compile();

            foreach (var kvp in this.Map(this.ReadEnumerable(reader, readerFunc)))
            {
                writerFunc(writer, kvp);
            }
        }

        public void RunCombineJob(IReader reader, IWriter writer)
        {
            this.CheckImplementsOne(j => j.Combine(null), j => j.CombineSingle(null));
            var readerFunc = Deserializer<KeyValuePair<TKeyIntermediate, TValueIntermediate>>.ReadExpression.Compile();
            var writerFunc = Serializer<KeyValuePair<TKeyIntermediate, TValueIntermediate>>.WriteExpression.Compile();

            foreach (var kvp in this.Combine(this.ReadGroupings(this.ReadEnumerable(reader, readerFunc))))
            {
                writerFunc(writer, kvp);
            }
        }

        public void RunReduceJob(IReader reader, IWriter writer)
        {
            this.CheckImplementsOne(j => j.Reduce(null), j => j.ReduceSingle(null));
            var readerFunc = Deserializer<KeyValuePair<TKeyIntermediate, TValueIntermediate>>.ReadExpression.Compile();
            var writerFunc = Serializer<KeyValuePair<TKeyOut, TValueOut>>.WriteExpression.Compile();

            foreach (var kvp in this.Reduce(this.ReadGroupings(this.ReadEnumerable(reader, readerFunc))))
            {
                writerFunc(writer, kvp);
            }
        }
        #endregion

        private IEnumerable<T> ReadEnumerable<T>(IReader reader, Func<IReader, T> readerFunc)
        {
            while (reader.HasMoreContent)
            {
                var value = readerFunc(reader);
                yield return value;
            }
        }

        private IEnumerable<IGrouping<TSomeKey, TSomeValue>> ReadGroupings<TSomeKey, TSomeValue>(IEnumerable<KeyValuePair<TSomeKey, TSomeValue>> pairs)
        {
            var comparer = EqualityComparer<TSomeKey>.Default;

            Grouping<TSomeKey, TSomeValue> currentGrouping = null;
            foreach (var kvp in pairs)
            {
                if (currentGrouping == null)
                {
                    currentGrouping = new Grouping<TSomeKey, TSomeValue>(kvp.Key);
                }
                else if (comparer.Equals(kvp.Key, currentGrouping.Key))
                {
                    currentGrouping.Values.Add(kvp.Value);
                }
                else
                {
                    yield return currentGrouping;
                    currentGrouping = new Grouping<TSomeKey, TSomeValue>(kvp.Key);
                }
            }

            yield return currentGrouping;
        }

        private void CheckImplementsOne(Expression<Action<BaseMapReduceJob<TKeyIn, TValueIn, TKeyIntermediate, TValueIntermediate, TKeyOut, TValueOut>>> methodExp1,
                                        Expression<Action<BaseMapReduceJob<TKeyIn, TValueIn, TKeyIntermediate, TValueIntermediate, TKeyOut, TValueOut>>> methodExp2)
        {
            var method1 = Helpers.Method(methodExp1);
            var method2 = Helpers.Method(methodExp2);
            Throw.InvalidIf(method1.DeclaringType == method2.DeclaringType, string.Format("{0} must implement exactly one of {1} and {2}", this.GetType().Name, method1.Name, method2.Name));
        }
    }

    internal class Grouping<TKey, TValue> : IGrouping<TKey, TValue>
    {
        public TKey Key { get; private set; }
        public List<TValue> Values { get; private set; }

        public Grouping(TKey key)
        {
            this.Key = key;
            this.Values = new List<TValue>();
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return this.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override string ToString()
        {
            var stringValue = string.Format("{0}: [{1}]", this.Key, string.Join(", ", this));
            return stringValue;
        }
    }
}
