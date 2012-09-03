using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using LinqToHadoop.Entities;

namespace LinqToHadoop.IO
{
    /// <summary>
    /// A class for writing values of a given type to an IWriter
    /// </summary>
    public class Serializer<T>
    {
        private static readonly Expression<Action<IWriter, T>> _writeExpression;

        public static Expression<Action<IWriter, T>> WriteExpression { get { return _writeExpression; } }

        static Serializer()
        {
            var writerParameter = Expression.Parameter(typeof(IWriter), "writer");
            var tParameter = Expression.Parameter(typeof(T), "t");
            var body = CreateWriteExpressionBody(writerParameter, tParameter);
            var lambda = Expression.Lambda<Action<IWriter, T>>(body, writerParameter, tParameter);
            Serializer<T>._writeExpression = lambda;
        }

        private static Expression CreateWriteExpressionBody(ParameterExpression writerParameter, ParameterExpression tParameter)
        {
            // first check if there is a direct write method for this type
            var method = GetWriteMethod(typeof(T));
            if (method != null)
            {
                var methodCall = Expression.Call(writerParameter, method, tParameter);
                return methodCall;
            }

            // for the none type, just do nothing!
            if (typeof(T) == typeof(None)) 
            {
                return Expression.Empty();
            }

            // for collections, we call BeginWritingCollection and then write each element
            if (typeof(T).IsGenericOfType(typeof(IEnumerable<>)))
            {

            }

            // otherwise, write out all properties
            var props = GetWriteProperties();
            var expressions = new List<Expression>();
            foreach (var pi in props)
            {
                // get the expression to read from the property
                var propertyValue = Expression.MakeMemberAccess(tParameter, pi);

                // get the write expression
                var writeExpression = (Expression)SerializerHelpers.GetExpressionMethod
                    .MakeGenericMethod(pi.PropertyType)
                    .Invoke(null, Type.EmptyTypes);
                
                // create the combined expression to write the value
                var writePropertyExpression = Expression.Invoke(writeExpression, writerParameter, propertyValue);
                expressions.Add(writePropertyExpression);
            }

            var writeAllPropertiesExpression = Expression.Block(expressions);
            return writeAllPropertiesExpression;
        }

        private static MethodInfo GetWriteMethod(Type type)
        {
            return typeof(IWriter).GetMethods()
                .SingleOrDefault(m => m.Name.StartsWith("Write") && m.GetParameters().Single().ParameterType == type);
        }

        private static IEnumerable<PropertyInfo> GetWriteProperties()
        {
            var isAnonymous = typeof(T).IsAnonymous();
            return typeof(T).GetProperties()
                // anonymous types have read-only properties
                .Where(pi => pi.CanRead && (isAnonymous || pi.CanWrite))
                // apply a standard ordering, since GetProperties() doesn't guarantee one
                .OrderBy(pi => pi.Name);
        }
    }

    internal static class SerializerHelpers
    {
        internal static readonly MethodInfo GetExpressionMethod = Helpers.Method<object>(_ => SerializerHelpers.GetExpression<int>())
            .GetGenericMethodDefinition();

        private static Expression<Action<IWriter, T>> GetExpression<T>()
        {
            return Serializer<T>.WriteExpression;
        }
    }
}
