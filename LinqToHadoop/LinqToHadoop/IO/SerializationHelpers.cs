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
            IEnumerable<PropertyInfo> propertiesToSerialize;

            IDictionary<ParameterInfo, PropertyInfo> mappings;
            var constructor = GetSerializationConstructor(type, out mappings);

            // if using a default constructor, serialize all writable properties
            if (!constructor.GetParameters().Any())
            {
                propertiesToSerialize = type.GetProperties()
                    .Where(pi => pi.CanWrite && pi.CanRead);
            }
            // otherwise, serialize all mapped properties
            else
            {
                propertiesToSerialize = mappings.Values;
            }
                
            // apply a standard ordering
            return propertiesToSerialize.OrderBy(pi => pi.Name);
        }

        public static ConstructorInfo GetSerializationConstructor(Type type, out IDictionary<ParameterInfo, PropertyInfo> mappings)
        {
            var properties = type.GetProperties()
                .Where(pi => pi.CanRead)
                .ToList();
            
            // if we have any writable properties, try to use a default constructor
            if (properties.Any(pi => pi.CanWrite))
            {
                var defaultConstructor = type.GetConstructor(Type.EmptyTypes);
                if (defaultConstructor != null)
                {
                    mappings = null;
                    return defaultConstructor;
                }
            }

            // otherwise, look for the constructor which matches the most properties
            var constructors = type.GetConstructors()
                .Select(c => new { Constructor = c, Mapping = GetConstructorParameterToPropertyMapping(c.GetParameters(), properties) })
                .Where(t => t.Mapping != null)
                .OrderByDescending(t => t.Mapping.Count);
            var bestConstructor = constructors.FirstOrDefault();
            Throw.If(bestConstructor == null, "No constructor found with parameters matching the properties of the type!");

            mappings = bestConstructor.Mapping;
            return bestConstructor.Constructor;
        }

        private static IDictionary<ParameterInfo, PropertyInfo> GetConstructorParameterToPropertyMapping(IList<ParameterInfo> parameters, IList<PropertyInfo> properties)
        {
            // not enough properties to get a mapping
            if (parameters.Count > properties.Count)
            {
                return null;
            }

            // do an exact join on name and type
            var byNameAndType = parameters.Join(properties, pa => new { type = pa.ParameterType, name = pa.Name.ToUpperInvariant() }, pi => new { type = pi.PropertyType, name = pi.Name.ToUpperInvariant() }, (pa, pi) => pa.WithValue(pi))
                .ToList();
            if (byNameAndType.Count == parameters.Count && !parameters.Except(byNameAndType.Select(kvp => kvp.Key)).Any())
            {
                return byNameAndType.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            return null;
        }

        public static LambdaExpression GetSerializerExpression(Type type)
        {
            return (LambdaExpression)typeof(Serializer<>)
                .MakeGenericType(type)
                .GetProperty("WriteExpression", BindingFlags.Public | BindingFlags.Static)
                .GetValue(null, null);
        }

        public static LambdaExpression GetDeserializerExpression(Type type)
        {
            return (LambdaExpression)typeof(Deserializer<>)
                .MakeGenericType(type)
                .GetProperty("ReadExpression", BindingFlags.Public | BindingFlags.Static)
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
