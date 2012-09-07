using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;

namespace LinqToHadoop.IO
{
    public static class SerializationHelpers
    {
        internal static readonly MethodInfo
            //WriteCollectionElementsMethod = Helpers.Method(() => SerializerHelpers.WriteCollectionElements<int>(null, null, null)),
            CountMethod = Helpers.Method(() => Enumerable.Count(string.Empty))
                .GetGenericMethodDefinition();

        public static IEnumerable<PropertyInfo> GetPropertiesToSerialize(Type type)
        {
            var isAnonymous = type.IsAnonymous();
            return type.GetProperties()
                // anonymous types have read-only properties
                .Where(pi => pi.CanRead && (isAnonymous || pi.CanWrite))
                // apply a standard ordering, since GetProperties() doesn't guarantee one
                .OrderBy(pi => pi.Name);
        }

        public static LambdaExpression GetSerializerExpression(Type type)
        {
            return (LambdaExpression)typeof(Serializer<>)
                .MakeGenericType(type)
                .GetProperty("WriteExpression", BindingFlags.Public | BindingFlags.Static)
                .GetValue(null, null);
        }

        //private static void WriteCollectionElements<T>(IEnumerable<T> collection, IWriter writer, Action<IWriter, T> writeAction)
        //{
        //    foreach (var element in collection)
        //    {
        //        writeAction(writer, element);
        //    }
        //}
    }
}
