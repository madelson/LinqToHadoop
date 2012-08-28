using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using LinqToHadoop.Reflection;
using System.Runtime.CompilerServices;

namespace LinqToHadoop
{
    public static class Helpers
    {
        public static MethodInfo Method<T>(Expression<Action<T>> methodCall)
        {
            var methodCallExpression = (MethodCallExpression)methodCall.Body;
            return methodCallExpression.Method;
        }

        public static MethodCallExpression Call(this MethodInfo @this, Expression instanceOrFirstParameter, params Expression[] parameters)
        {
            var parametersToUse = !@this.IsStatic
                ? parameters
                : new[] { instanceOrFirstParameter }.Concat(parameters)
                    .Where(e => e != null);
            var expression = Expression.Call(
                instance: !@this.IsStatic ? instanceOrFirstParameter : null,
                method: @this.IsGenericMethod
                    ? @this.MakeGenericMethodFromParameters(parametersToUse.Select(e => e.Type).ToList())
                    : @this,
                arguments: parametersToUse
            );
            return expression;
        }

        public static int GetHashCode<T>(T obj)
        {
            var hash = obj != null
                ? obj.GetHashCode()
                : 0;
            return hash;
        }

        public static bool IsAnonymous(this Type @this)
        {
            // HACK: The only way to detect anonymous types right now.
            var isAnonymous = Attribute.IsDefined(@this, typeof(CompilerGeneratedAttribute), false)
                && @this.IsGenericType && @this.Name.Contains("AnonymousType")
                && (@this.Name.StartsWith("<>") || @this.Name.StartsWith("VB$"))
                && (@this.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
            return true;
        }
    }
}
