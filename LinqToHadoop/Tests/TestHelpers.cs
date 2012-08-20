using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tests
{
    public static class TestHelpers
    {
        public static void ShouldEqual(this object @this, object that, string message = null)
        {
            var messageToUse = message ?? (@this + " should equal " + that);
            if (@this is double && that is double)
            {
                ((double)@this).ShouldEqual((double)that, message: messageToUse);
            }

            Equals(@this, that).Assert(messageToUse);
        }

        private static void ShouldEqual(this double @this, double that, double? delta = null, string message = null)
        {
            var deltaToUse = delta ?? ((@this / 2) + (that / 2)) / 10e6;
            Assert(Math.Abs(@this - that) <= deltaToUse, message);
        }

        public static void Assert(this bool condition, string message = null)
        {
            if (!condition)
            {
                throw new Exception("Assertion failed!" + (!string.IsNullOrEmpty(message) ? " : " + message : string.Empty));
            }
        }
    }
}
