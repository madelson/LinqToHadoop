using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToHadoop.IO
{
    public interface IDecoder
    {
        byte ReadByte(IoType type);
        char ReadChar(IoType type);
        int ReadInt(IoType type);
        long ReadLong(IoType type);
        double ReadDouble(IoType type);
        DateTime ReadDateTime(IoType type);
        string ReadString(IoType type);
    }
}
