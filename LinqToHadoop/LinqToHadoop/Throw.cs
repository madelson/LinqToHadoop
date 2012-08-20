using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToHadoop
{
    public static class Throw
    {
        public static InvalidOperationException ShouldNeverGetHere(string message = null)
        {
            throw new InvalidOperationException(message ?? "Should never get here!");
        }

        public static InvalidOperationException InvalidEnumValue<TEnum>(TEnum value, string message = null)
            where TEnum : struct
        {
            throw Throw.ShouldNeverGetHere(string.Format("{0}Unexpected value {1} for {2}", message != null ? message + ": " : string.Empty, value, typeof(TEnum).Name));
        }

        public static void InvalidIf(bool condition, string message = null)
        {
            if (condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        public static void NotSupportedIf(bool condition, string message = null)
        {
            if (condition)
            {
                throw new NotSupportedException(message);
            }
        }

        public static void If(bool condition, string message = null)
        {
            if (condition)
            {
                throw new ArgumentException(message ?? string.Empty);
            }
        }
    }
}
