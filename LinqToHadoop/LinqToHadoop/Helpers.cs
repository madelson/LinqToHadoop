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
        public static readonly object[] EmptyArgs = new object[0];

        public static MethodInfo Method<T>(Expression<Action<T>> methodCall)
        {
            var methodCallExpression = (MethodCallExpression)methodCall.Body;
            return methodCallExpression.Method;
        }

        public static MethodInfo Method(Expression<Action> methodCall)
        {
            var methodCallExpression = (MethodCallExpression)methodCall.Body;
            return methodCallExpression.Method;
        }

        public static MethodInfo GetMethod<T>(this T @this, Expression<Action<T>> methodCall)
        {
            return Method(methodCall);
        }

        public static PropertyInfo Property<TProperty>(Expression<Func<TProperty>> propertyAccess)
        {
            var propertyExpression = (MemberExpression)propertyAccess.Body;
            return (PropertyInfo)propertyExpression.Member;
        }

        public static MethodCallExpression Call(this MethodInfo @this, Expression instanceOrFirstParameter = null, params Expression[] parameters)
        {
            var parametersToUse = !@this.IsStatic
                ? parameters
                : new[] { instanceOrFirstParameter }.Where(e => e != null).Concat(parameters);
            var expression = Expression.Call(
                instance: !@this.IsStatic ? instanceOrFirstParameter : null,
                method: @this.IsGenericMethod
                    ? @this.MakeGenericMethodFromParameters(parametersToUse.Select(e => e.Type).ToList())
                    : @this,
                arguments: parametersToUse
            );
            return expression;
        }

        public static object InferGenericsAndInvoke(this MethodInfo @this, object instanceOrFirstParameter = null, params object[] parameters)
        {
            var parametersToUse = !@this.IsStatic
                ? parameters
                : new[] { instanceOrFirstParameter }.Where(e => e != null).Concat(parameters);
            var method = @this.IsGenericMethod
                ? @this.MakeGenericMethodFromParameters(parametersToUse.Select(e => e.GetType()).ToList())
                : @this;
            var result = method.Invoke(
                obj: !@this.IsStatic ? instanceOrFirstParameter : null,
                parameters: parametersToUse.ToArray()
            );
            return result;
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

        public static KeyValuePair<TKey, TValue> WithValue<TKey, TValue>(this TKey @this, TValue value)
        {
            return new KeyValuePair<TKey, TValue>(@this, value);
        }

        public static KeyValuePair<TKey, TValue> WithKey<TKey, TValue>(this TValue @this, TKey key)
        {
            return key.WithValue(@this);
        }

        public static Type[] GetGenericArguments(this Type @this, Type genericTypeDefinition)
        {
            Throw.If(!genericTypeDefinition.IsGenericTypeDefinition, "generic type definition required");

            // if @this directly matches, use it's type arguments
            if (@this.IsGenericType && @this.GetGenericTypeDefinition() == genericTypeDefinition)
            {
                var arguments = @this.GetGenericArguments();
                return arguments;
            }

            // if any interfaces match, use the matching interface
            if (genericTypeDefinition.IsInterface)
            {
                var matchingInterfaceArguments = @this.GetInterfaces()
                    .Select(i => i.GetGenericArguments(genericTypeDefinition))
                    .FirstOrDefault(a => a.Length > 0);
                if (matchingInterfaceArguments != null)
                {
                    return matchingInterfaceArguments;
                }
            }

            // finally, check the base type if we have one
            if (@this.BaseType != null && @this.BaseType != typeof(object))
            {
                return @this.BaseType.GetGenericArguments(genericTypeDefinition);
            }

            // otherwise, failure
            return Type.EmptyTypes;
        }

        public static bool IsGenericOfType(this Type @this, Type genericTypeDefinition)
        {
            var arguments = @this.GetGenericArguments(genericTypeDefinition);
            return arguments.Length > 0;
        }

        public static bool Is(this Type @this, Type that)
        {
            var isThisAThat = that.IsAssignableFrom(@this);
            return isThisAThat;
        }
    }
}
