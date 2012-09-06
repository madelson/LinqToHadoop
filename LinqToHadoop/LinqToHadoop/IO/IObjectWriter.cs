using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToHadoop.IO
{
    public interface IObjectWriter
    {
        void Write<T>(T value);
    }
}
