using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToHadoop.IO
{
    public interface IReader : IDisposable
    {
        void BeginReadingKey();
        void BeginReadingValue();
        int BeginReadingCollection();

        byte ReadByte();
        char ReadChar();
        int ReadInt();
        long ReadLong();
        double ReadDouble();
        DateTime ReadDateTime();
        string ReadString();
    }

    public interface IWriter : IDisposable
    {
        void BeginWritingKey();
        void BeginWritingValue();
        void BeginWritingCollection(int count);

        void WriteByte(byte value);
        void WriteChar(char value);
        void WriteInt(int value);
        void WriteLong(long value);
        void WriteDouble(double value);
        void WriteDateTime(DateTime value);
        void WriteString(string value);
    }
}
