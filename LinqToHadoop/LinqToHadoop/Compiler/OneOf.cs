using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToHadoop.Compiler
{
    /// <summary>
    /// A structure used with joins to maintain strong typing
    /// </summary>
    public struct OneOf<T1, T2> : IEquatable<OneOf<T1, T2>>
    {
        private readonly T1 _item1;
        private readonly T2 _item2;

        public OneOf(T1 item1, T2 item2, int type)
            : this()
        {
            Throw.If(type != 1 && type != 2);
            this.Type = type;
            this._item1 = item1;
            this._item2 = item2;
        }

        public int Type { get; private set; }

        public object Item
        {
            get
            {
                return this.Type == 1
                    ? this.Item1
                    : (object)this.Item2;
            }
        }

        public T1 Item1
        {
            get
            {
                Throw.If(this.Type != 1);
                return this._item1;
            }
        }

        public T2 Item2
        {
            get
            {
                Throw.If(this.Type != 2);
                return this._item2;
            }
        }

        public bool Equals(OneOf<T1, T2> other)
        {
            var equals = this.Type == other.Type
                && Equals(this.Item, other.Item);
            return equals;
        }

        public override bool Equals(object obj)
        {
            var equals = obj is OneOf<T1, T2>
                && this.Equals((OneOf<T1, T2>)obj);
            return equals;
        }

        public override int GetHashCode()
        {
            var hash = this.Type == 1
                ? Helpers.GetHashCode(this.Item1)
                : Helpers.GetHashCode(this.Item2);
            return hash;
        }
    }
}
