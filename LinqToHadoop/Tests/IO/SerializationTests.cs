using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToHadoop.IO;

namespace Tests.IO
{
    public static class SerializationTests
    {
        public static void RunAll()
        {
            var results = TestSerialize(5);
            results.Single().ShouldEqual(5);

            results = TestSerialize(new { a = DateTime.MinValue, b = "hi" });
            results.SequenceEqual(new object[] { DateTime.MinValue, "hi" })
                .Assert();
        }

        public static List<object> TestSerialize<T>(T value)
        {
            var writer = new ListWriter();
            var serializer = Serializer<T>.WriteExpression.Compile();
            serializer(writer, value);
            return writer.Objects;
        }

        public class ListWriter : IWriter
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
                throw new NotImplementedException();
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
        }
    }
}
