using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace LinqToHadoop.Query
{
    /// <summary>
    /// The IQueryable[T] implementation for LinqToHadoop
    /// </summary>
    public class HadoopQueryable<T> : IQueryable<T>
    {
        private readonly HadoopQueryProvider _provider;

        public HadoopQueryable(HadoopQueryProvider provider, Expression expression) 
        {
            this._provider = provider;
            this.Expression = expression;
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public Type ElementType { get { return typeof(T); } }

        public Expression Expression { get; private set; }

        public IQueryProvider Provider
        {
            get { return this._provider; }
        }
    }
}
