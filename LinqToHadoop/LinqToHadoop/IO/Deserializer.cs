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
    /// A class for reading values given an IReader
    /// </summary>
    public class Deserializer<T>
    {
        private static readonly Expression<Action<IReader, T>> _readExpression;

        public static Expression<Action<IReader, T>> ReadExpression { get { return _readExpression; } }

        static Deserializer()
        {
            var readerParameter = Expression.Parameter(typeof(IReader), "writer");
            var tParameter = Expression.Parameter(typeof(T), "t");
            var body = CreateReadExpressionBody(readerParameter, tParameter);
            var lambda = Expression.Lambda<Action<IReader, T>>(body, readerParameter, tParameter);
            Deserializer<T>._readExpression = lambda;
        }

        private static Expression CreateReadExpressionBody(ParameterExpression readerParameter, ParameterExpression tParameter)
        {
            // first check if there is a direct read method for this type
            var method = GetReadMethod(typeof(T));
            if (method != null)
            {
                var methodCall = Expression.Call(readerParameter, method, tParameter);
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
                    readerParameter,
                    Helpers.Method(() => Enumerable.Count<int>(null)).Call(enumerableTParameter)
                );

                // create an expression for looping over all elements in the collection and writing them out
                var writeElementExpression = SerializationHelpers.GetSerializerExpression(enumerableElementType);
                // this works, but requires a lot of independently compiled expressions. I'm going for a hard-coded foreach loop
                // instead. Not compiling the expression does something even weirder: I get weird NullReferenceExceptions!
                //var writeAllElementsExpression = SerializerHelpers.WriteCollectionElementsMethod.Call(
                //    enumerableTParameter,
                //    writerParameter,
                //    Expression.Constant(writeElementExpression.Compile())
                //);
                var writeAllElementsExpression = ExpressionHelpers.ForEachLoop(
                    enumerable: enumerableTParameter,
                    bodyFactory: (element, index, label) => Expression.Invoke(writeElementExpression, readerParameter, element)
                );

                // return the two generated expressions in sequence
                return Expression.Block(
                    beginWritingCollection,
                    writeAllElementsExpression
                );
            }

            // otherwise, write out all properties
            var props = SerializationHelpers.GetPropertiesToSerialize(typeof(T));
            var expressions = new List<Expression>();
            foreach (var pi in props)
            {
                // get the expression to read from the property
                var propertyValue = Expression.MakeMemberAccess(tParameter, pi);

                // get the write expression
                var writeExpression = SerializationHelpers.GetSerializerExpression(pi.PropertyType);

                // create the combined expression to write the value
                var writePropertyExpression = Expression.Invoke(writeExpression, readerParameter, propertyValue);
                expressions.Add(writePropertyExpression);
            }

            var writeAllPropertiesExpression = Expression.Block(expressions);
            return writeAllPropertiesExpression;
        }

        private static MethodInfo GetReadMethod(Type type)
        {
            return typeof(IReader).GetMethods()
                .SingleOrDefault(m => m.Name.StartsWith("Read") && m.GetParameters().Single().ParameterType == type);
        }
    }
}
