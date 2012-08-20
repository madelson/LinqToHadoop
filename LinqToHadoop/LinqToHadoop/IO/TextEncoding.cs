using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LinqToHadoop.IO
{
    public class TextEncodingEscaper
    {
        public const char DefaultSeparator = '\t';
        private static readonly string KeyEscape = char.MaxValue + string.Empty + char.MaxValue;

        public string Separator { get; private set; }
        private readonly string _keyEscapeSubstitute, _keySeparatorSubstitute, _keyNewlineSubstitute,
            _valueEscape, _valueEscapeSubstitute, _valueSeparatorSubstitute, _valueNewlineSubstitute;

        public TextEncodingEscaper(char separator = DefaultSeparator)
        {
            Throw.If(separator <= (char)1 || separator >= char.MaxValue - 1, "separator must be between 1 and char.MaxValue - 1");
            Throw.If(separator == '\n', "separator must not be the newline character");

            this.Separator = separator.ToString();

            /*
             * The idea here is to substitute in a string such that neither the key nor
             * the value are still in the result but the result maintains lexicographical ordering
             * compared to other results. The method is to replace an instance of a char to be escaped
             * with that char - 1, char.MaxValue. char.MaxValue is already escaped by replacing it
             * with char.MaxValue - 1, char.MaxValue. The one edge case is when separator - 1 = newline
             * or vice versa, since then the escaping of one leaves the other in the string. To handle that
             * case, we sometimes escape as ch => ch - 2, char.MaxValue, char.MaxValue instead.
             */
            this._keySeparatorSubstitute = (char)(this.Separator[0] - 1) != '\n'
                ? string.Concat((char)(this.Separator[0] - 1), char.MaxValue)
                : string.Concat((char)(this.Separator[0] - 2), char.MaxValue, char.MaxValue);
            this._keyNewlineSubstitute = (char)('\n' - 1) != this.Separator[0]
                ? string.Concat((char)('\n' - 1), char.MaxValue)
                : string.Concat((char)('\n' - 2), char.MaxValue, char.MaxValue);
            this._keyEscapeSubstitute = string.Concat((char)(char.MaxValue - 1), char.MaxValue);

            this._valueEscape = this.Separator != "#" ? "#" : "?";
            this._valueEscapeSubstitute = this._valueEscape + (this.Separator != "E" ? "E" : "e");
            this._valueSeparatorSubstitute = this._valueEscape + (this.Separator != "S" ? "S" : "s");
            this._valueNewlineSubstitute = this._valueEscape + (this.Separator != "N" ? "N" : "n");
        }

        public string EscapeKeyString(string keyString)
        {
            var escaped = keyString.Replace(KeyEscape, this._keyEscapeSubstitute)
                .Replace(this.Separator, this._keySeparatorSubstitute)
                .Replace("\n", this._keyNewlineSubstitute);
            return escaped;
        }

        public string EscapeValueString(string valueString)
        {
            var escaped = valueString.Replace(this._valueEscape, this._valueEscapeSubstitute)
                .Replace(this.Separator, this._valueSeparatorSubstitute)
                .Replace("\n", this._valueNewlineSubstitute);
            return escaped;
        }

        public string UnescapeKeyString(string keyString)
        {
            var unescaped = keyString.Replace(this._keyNewlineSubstitute, "\n")
                .Replace(this._keySeparatorSubstitute, this.Separator)
                .Replace(this._keyEscapeSubstitute, KeyEscape);
            return unescaped;
        }

        public string UnescapeValueString(string valueString)
        {
            var unescaped = valueString.Replace(this._valueNewlineSubstitute, "\n")
                .Replace(this._valueSeparatorSubstitute, this.Separator)
                .Replace(this._valueEscapeSubstitute, this._valueEscape);
            return unescaped;
        }
    }

    public class TextEncoder : IEncoder
    {
        private readonly TextWriter _writer;
        private readonly TextEncodingEscaper _escaper;

        public TextEncoder(TextWriter writer, char separator = TextEncodingEscaper.DefaultSeparator)
        {
            this._writer = writer;
            this._escaper = new TextEncodingEscaper(separator);
        }

        public void WriteByte(byte value, IoType type)
        {
            this.WriteString(value.ToString(), type);
        }

        public void WriteChar(char value, IoType type)
        {
            this.WriteString(value.ToString(), type);
        }

        public void WriteInt(int value, IoType type)
        {
            this.WriteString(value.ToString(), type);
        }

        public void WriteLong(long value, IoType type)
        {
            this.WriteString(value.ToString(), type);
        }

        public void WriteDouble(double value, IoType type)
        {
            this.WriteString(value.ToString(), type);
        }

        public void WriteDateTime(DateTime value, IoType type)
        {
            // TODO improve this
            this.WriteString(value.ToString(), type);
        }

        public void WriteString(string value, IoType type)
        {
            string safeValue;
            switch (type)
            {
                case IoType.Raw:
                    safeValue = value;
                    break;
                case IoType.Key:
                    safeValue = this._escaper.EscapeKeyString(value);
                    break;
                case IoType.Value:
                    safeValue = this._escaper.EscapeValueString(value);
                    break;
                default:
                    throw Throw.InvalidEnumValue(type);
            }

            this._writer.Write(safeValue);
        }
    }

    public class TextDecoder : IDecoder
    {
        private readonly TextReader _reader;
        private readonly TextEncodingEscaper _escaper;
        private readonly StringBuilder _buffer = new StringBuilder();

        public TextDecoder(TextReader reader, char separator = TextEncodingEscaper.DefaultSeparator)
        {
            this._reader = reader;
            this._escaper = new TextEncodingEscaper(separator);
        }

        public byte ReadByte(IoType type)
        {
            return byte.Parse(this.ReadString(type));
        }

        public char ReadChar(IoType type)
        {
            var value = this.ReadString(type);
            Throw.InvalidIf(value.Length != 1, "returned value string was not of length 1!");
            return value[0];
        }

        public int ReadInt(IoType type)
        {
            return int.Parse(this.ReadString(type));
        }

        public long ReadLong(IoType type)
        {
            return long.Parse(this.ReadString(type));
        }

        public double ReadDouble(IoType type)
        {
            return double.Parse(this.ReadString(type));
        }

        public DateTime ReadDateTime(IoType type)
        {
            return DateTime.Parse(this.ReadString(type));
        }

        public string ReadString(IoType type)
        {
            // fill the buffer with chars until we hit EOF, the separator, or \n
            this._buffer.Length = 0;
            int ch = this._reader.Read();
            while (ch != -1 && (char)ch != this._escaper.Separator[0] && (char)ch != '\n') {
                this._buffer.Append((char)ch);
                ch = this._reader.Read();
            }
            var rawValue = this._buffer.ToString();

            switch (type)
            {
                case IoType.Key:
                    return this._escaper.UnescapeKeyString(rawValue);
                case IoType.Value:
                    return this._escaper.UnescapeValueString(rawValue);
                case IoType.Raw:
                    return rawValue;
                default:
                    throw Throw.InvalidEnumValue(type);
            }
        }
    }
}
