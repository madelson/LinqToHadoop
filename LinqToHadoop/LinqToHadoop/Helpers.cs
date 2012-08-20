using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;

namespace LinqToHadoop
{
    public static class Helpers
    {
        public static MethodInfo Method<T>(Expression<Action<T>> methodCall)
        {
            var methodCallExpression = (MethodCallExpression)methodCall.Body;
            return methodCallExpression.Method;
        }

        public static int GetHashCode<T>(T obj)
        {
            var hash = obj != null
                ? obj.GetHashCode()
                : 0;
            return hash;
        }
    }
}
