using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using LinqToHadoop.Entities;
using LinqToHadoop.Compiler;

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
            var enumerableElementType = typeof(T).GetGenericArguments(typeof(IEnumerable<>)).SingleOrDefault();
            if (enumerableElementType != null)
            {
                // for type inference to work properly, cast to IEnumerable so the expression must be a generic type (rather than T[])
                var enumerableTParameter = Expression.Convert(tParameter, typeof(IEnumerable<>).MakeGenericType(enumerableElementType));

                // create an expression for writing the collection header (basically, the count
                Expression<Action<IWriter, int>> callBeginWritingCollection = (w, count) => w.BeginWritingCollection(count);
                var beginWritingCollection = Expression.Invoke(
                    callBeginWritingCollection,
                    writerParameter,
                    Helpers.Method(() => Enumerable.Count<int>(null)).Call(enumerableTParameter)
                );

                // create an expression for looping over all elements in the collection and writing them out
                var writeElementExpression = (LambdaExpression)SerializerHelpers.GetExpressionMethod
                    .MakeGenericMethod(enumerableElementType)
                    .Invoke(null, Helpers.EmptyArgs);
                var writeAllElementsExpression = SerializerHelpers.WriteCollectionElementsMethod.Call(
                    enumerableTParameter,
                    writerParameter,
                    Expression.Constant(writeElementExpression.Compile())
                );

                // return the two generated expressions in sequence
                return Expression.Block(
                    beginWritingCollection,
                    writeAllElementsExpression
                );
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
                    .Invoke(null, Helpers.EmptyArgs);
                
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
        internal static readonly MethodInfo GetExpressionMethod = Helpers.Method(() => SerializerHelpers.GetExpression<int>())
                .GetGenericMethodDefinition(),
            WriteCollectionElementsMethod = Helpers.Method(() => SerializerHelpers.WriteCollectionElements<int>(null, null, null)),
            CountMethod = Helpers.Method(() => Enumerable.Count(string.Empty))
                .GetGenericMethodDefinition();

        private static Expression<Action<IWriter, T>> GetExpression<T>()
        {
            return Serializer<T>.WriteExpression;
        }

        private static void WriteCollectionElements<T>(IEnumerable<T> collection, IWriter writer, Action<IWriter, T> writeAction)
        {
            foreach (var element in collection)
            {
                writeAction(writer, element);
            }
        }
    }
}
