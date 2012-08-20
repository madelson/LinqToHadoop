using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToHadoop
{
    public interface ILinqToHadoopContext
    {
        IQueryable<T> Query<T>();
    }
}
