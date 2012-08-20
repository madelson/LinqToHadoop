using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LinqToHadoop.Entities
{
    public interface IStreamingWritable<T>
        where T : IStreamingWritable<T>
    {
        void ReadFields(string inputLine);
        void Write(TextWriter writer);
    }
}
