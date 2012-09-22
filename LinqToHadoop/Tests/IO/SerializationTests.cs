using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToHadoop;
using LinqToHadoop.IO;
using LinqToHadoop.Entities;

namespace Tests.IO
{
    public static class SerializationTests
    {
        public static void RunAll()
        {
            TestSerializer();
            TestDeserializer();
            TestSerializerAndDeserializer();
        }

        public static void TestSerializer()
        {
            var results = TestSerialize(5);
            results.Single().ShouldEqual(5);

            results = TestSerialize(new { a = DateTime.MinValue, b = "hi" });
            results.SequenceEqual(new object[] { DateTime.MinValue, "hi" })
                .Assert();

            results = TestSerialize(new[] { 1, 2, 3, 4 });
            results.SequenceEqual(new object[] { 4, 1, 2, 3, 4 })
                .Assert();

            results = TestSerialize(new { a = new { b = 1, c = 2 }, d = 3 });
            results.SequenceEqual(new object[] { 1, 2, 3 }).Assert();

            results = TestSerialize(new Dictionary<string, int> { { "x", 7 } });
            results.SequenceEqual(new object[] { 1, "x", 7 }).Assert();

            results = TestSerialize(new List<int[]> { new[] { 7, 8 }, new[] { 70, 80 } });
            results.SequenceEqual(new object[] { 2, 2, 7, 8, 2, 70, 80 }).Assert();

            results = TestSerialize(new Dictionary<string, int[]> { { "s", new[] { 20, 10 } } });
            results.SequenceEqual(new object[] { 1, "s", 2, 20, 10 }).Assert();

            results = TestSerialize(new { "abc".Length });
            results.SequenceEqual(new object[] { 3 }).Assert();

            results = TestSerialize(new { a = new[] { 20, 10 }, b = 2 });
            results.SequenceEqual(new object[] { 2, 20, 10, 2 }).Assert();

            results = TestSerialize(new { dict = new Dictionary<string, int[]> { { "s", new[] { 20, 10 } } }, dictX = new { "abc".Length } });
            results.SequenceEqual(new object[] { 1, "s", 2, 20, 10, 3 })
                .Assert();

            // bulk write
            var aLot = Enumerable.Range(0, 10000).Select(i => (i * i).WithValue(i.ToString())).ToList();
            var serializer = Serializer<KeyValuePair<int, string>>.WriteExpression.Compile();
            var writer = new ListReaderWriter();
            aLot.ForEach(kvp => serializer(writer, kvp));
            writer.Objects.Count.ShouldEqual(aLot.Count * 2);
        }

        public static List<object> TestSerialize<T>(T value)
        {
            var writer = new ListReaderWriter();
            var serializer = Serializer<T>.WriteExpression.Compile();
            serializer(writer, value);
            return writer.Objects;
        }

        public static void TestDeserializer()
        {
            var kvp = TestDeserialize<KeyValuePair<int, int>>(50, -2);
            kvp.Key.ShouldEqual(50);
            kvp.Value.ShouldEqual(-2);

            var kvp2 = TestDeserialize<KeyValuePair<IDictionary<string, int>, Data>>(2, "a", 7, "b", 6, -1000, "c");
            kvp2.Key.Count.ShouldEqual(2);
            kvp2.Key["a"].ShouldEqual(7);
            kvp2.Key["b"].ShouldEqual(6);
            kvp2.Value.A.ShouldEqual(-1000);
            kvp2.Value.B.ShouldEqual("c");
            kvp2.Value.C.ShouldEqual(None.Instance);

            // bulk read
            var aLot = Enumerable.Range(0, 2000).Select(i => (i * i).WithValue(i.ToString().ToArray())).ToList();
            var deserializer = Deserializer<KeyValuePair<int, char[]>>.ReadExpression.Compile();
            var reader = new ListReaderWriter();
            aLot.ForEach(kvp3 => reader.Objects.AddRange(new object[] { kvp3.Key, kvp3.Value.Length }.Concat(kvp3.Value.Cast<object>())));
            var aLotCopy = aLot.Take(0).ToList();
            for (int i = 0; i < aLot.Count; ++i)
            {
                aLotCopy.Add(deserializer(reader));
            }
            aLot.Zip(aLotCopy, (orig, cpy) => orig.Key == cpy.Key && new string(orig.Value) == new string(cpy.Value))
                .All(b => b)
                .Assert();
        }

        public static T TestDeserialize<T>(params object[] values)
        {
            var reader = new ListReaderWriter();
            reader.Objects.AddRange(values);
            var deserializer = Deserializer<T>.ReadExpression.Compile();
            var result = deserializer(reader);
            return result;
        }

        public static void TestSerializerAndDeserializer()
        {
            var res1 = TestRoundTrip(2);
            res1.ShouldEqual(2);

            var res2 = TestRoundTrip(new { a = 5, b = new { c = 6, d = "7" } });
            res2.ShouldEqual(new { a = 5, b = new { c = 6, d = "7" } });

            var tuple = Tuple.Create(7, new HashSet<int> { 1, 2 });
            var res3 = TestRoundTrip(tuple);
            res3.Item1.ShouldEqual(tuple.Item1);
            res3.Item2.Except(tuple.Item2).Count().ShouldEqual(0);
        }

        public static T TestRoundTrip<T>(T value)
        {
            var rw = new ListReaderWriter();
            var serializer = Serializer<T>.WriteExpression.Compile();
            var deserializer = Deserializer<T>.ReadExpression.Compile();
            serializer(rw, value);
            var outValue = deserializer(rw);
            return outValue;
        }

        public class ListReaderWriter : IWriter, IReader
        {
            public readonly List<object> Objects = new List<object>();

            public void BeginWritingKey()
            {
                throw new NotImplementedException();
            }

            public void BeginWritingValue()
            {
                throw new NotImplementedException();
            }

            public void BeginWritingCollection(int count)
            {
                this.WriteInt(count);
            }

            public void WriteByte(byte value)
            {
                this.Objects.Add(value);
            }

            public void WriteChar(char value)
            {
                this.Objects.Add(value);
            }

            public void WriteInt(int value)
            {
                this.Objects.Add(value);
            }

            public void WriteLong(long value)
            {
                this.Objects.Add(value);
            }

            public void WriteDouble(double value)
            {
                this.Objects.Add(value);
            }

            public void WriteDateTime(DateTime value)
            {
                this.Objects.Add(value);
            }

            public void WriteString(string value)
            {
                this.Objects.Add(value);
            }

            public void Dispose()
            {
            }

            public void BeginReadingKey()
            {
                throw new NotImplementedException();
            }

            public void BeginReadingValue()
            {
                throw new NotImplementedException();
            }

            public int BeginReadingCollection()
            {
                return this.ReadInt();
            }

            public byte ReadByte()
            {
                return this.Consume<byte>();   
            }

            public char ReadChar()
            {
                return this.Consume<char>();
            }

            public int ReadInt()
            {
                return this.Consume<int>();
            }

            public long ReadLong()
            {
                return this.Consume<long>();
            }

            public double ReadDouble()
            {
                return this.Consume<double>();
            }

            public DateTime ReadDateTime()
            {
                return this.Consume<DateTime>();
            }

            public string ReadString()
            {
                return this.Consume<string>();
            }

            private T Consume<T>()
            {
                var value = this.Objects[0];
                this.Objects.RemoveAt(0);

                return (T)value;
            }
        }

        private class Data
        {
            public int A { get; set; }
            public string B { get; set; }
            public None C { get; set; }
        }
    }
}
