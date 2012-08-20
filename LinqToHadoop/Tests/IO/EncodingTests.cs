using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToHadoop.IO;
using System.IO;

namespace Tests.IO
{
    public static class EncodingTests
    {
        public static void TestTextEscapes()
        {
            const string Separators = ",a#SsNn?\t";
            var testStrings = new[] {
                "aaa",
                "aab",
                "baa",
                "aaaa",
                "a" + char.MaxValue,
                "a" + (char)(char.MaxValue - 1),
                "a" + char.MaxValue + ",",
                "Sn#S\nab,c",
                "#?E#?e\n###?>??ee",
                "\n",
                "\t",
            }
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList()
            .AsReadOnly();

            foreach (var sep in Separators)
            {
                var escaper = new TextEncodingEscaper(sep);

                // test you can escape and unescape
                foreach (var s in testStrings)
                {
                    var keyEscaped = escaper.EscapeKeyString(s);
                    keyEscaped.Contains(sep).ShouldEqual(false);
                    keyEscaped.Contains('\n').ShouldEqual(false);
                    var keyUnescaped = escaper.UnescapeKeyString(keyEscaped);
                    keyUnescaped.ShouldEqual(s);

                    var valueEscaped = escaper.EscapeValueString(s);
                    valueEscaped.Contains(sep).ShouldEqual(false);
                    valueEscaped.Contains('\n').ShouldEqual(false);
                    var valueUnescaped = escaper.UnescapeValueString(valueEscaped);
                    valueUnescaped.ShouldEqual(s);
                }

                // test that ordering is preserved for keys
                var reorderdEscaped = testStrings.Reverse()
                    .Select(escaper.EscapeKeyString)
                    .OrderBy(s => s, StringComparer.Ordinal)
                    .ToList()
                    .AsReadOnly();
                reorderdEscaped.Select(escaper.UnescapeKeyString)
                    .SequenceEqual(testStrings)
                    .Assert(testStrings + " != " + reorderdEscaped);
            }
        }

        public static void TestTextEncoder()
        {
            var values = new object[] 
            {
                2,
                '\n',
                TextEncodingEscaper.DefaultSeparator,
                long.MaxValue,
                int.MinValue,
                double.MaxValue / 2,
                -double.Epsilon,
                DateTime.Now,
                "\ne#$",
                "\t"
            };

            foreach (var value in values)
            {
                foreach (var type in new[] { IoType.Key, IoType.Value })
                {
                    var stream = new StreamContext();
                    var encoder = new TextEncoder(stream.writer);
                    var decoder = new TextDecoder(stream.reader);
                    Action f = () => encoder.WriteChar(TextEncodingEscaper.DefaultSeparator, IoType.Raw);

                    TestEncoderHelper(value, encoder, decoder, stream, type, f);
                }
            }
        }

        private static void TestEncoderHelper(object value, IEncoder encoder, IDecoder decoder, StreamContext stream, IoType type, Action afterEncode = null)
        {
            var inMethod = typeof(IEncoder).GetMethods()
                .First(m => m.Name.StartsWith("Write") && m.GetParameters()[0].ParameterType == value.GetType());
            var outMethod = typeof(IDecoder).GetMethods()
                .First(m => m.Name.StartsWith("Read") && m.ReturnType == value.GetType());

            // write it in
            inMethod.Invoke(encoder, new[] { value, type });
            if (afterEncode != null)
            {
                afterEncode();
            }

            // reset the stream for reading
            stream.writer.Flush();
            stream.stream.Flush();
            stream.stream.Seek(0, SeekOrigin.Begin);

            // read it out
            var outValue = outMethod.Invoke(decoder, new object[] { type });

            outValue.ShouldEqual(value);
        }

        public static void RunAll()
        {
            TestTextEscapes();
            TestTextEncoder();
        }

        private class StreamContext
        {
            public readonly MemoryStream stream = new MemoryStream();
            public readonly StreamWriter writer;
            public readonly StreamReader reader;

            public StreamContext()
            {
                this.writer = new StreamWriter(this.stream);
                this.reader = new StreamReader(this.stream);
            }
        }
    }
}
