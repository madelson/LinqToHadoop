using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToHadoop;
using LinqToHadoop.IO;

namespace Tests.IO
{
    public static class SerializationTests
    {
        public static void RunAll()
        {
            TestSerializer();
            TestDeserializer();
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
            kvp.Value.ShouldEqual(50);
        }

        public static T TestDeserialize<T>(params object[] values)
        {
            var reader = new ListReaderWriter();
            reader.Objects.AddRange(values);
            var deserializer = Deserializer<T>.ReadExpression.Compile();
            var result = deserializer(reader);
            return result;
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
    }
}
