using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToHadoop.IO
{
    interface IEncoder
    {
        void WriteByte(byte value, IoType type);
        void WriteChar(char value, IoType type);
        void WriteInt(int value, IoType type);
        void WriteLong(long value, IoType type);
        void WriteDouble(double value, IoType type);
        void WriteDateTime(DateTime value, IoType type);
        void WriteString(string value, IoType type);
    }
}
