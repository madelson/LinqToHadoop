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
        private static readonly Expression<Func<IReader, T>> _readExpression;

        public static Expression<Func<IReader, T>> ReadExpression { get { return _readExpression; } }

        static Deserializer()
        {
            var readerParameter = Expression.Parameter(typeof(IReader), "reader");
            var body = CreateReadExpressionBody(readerParameter);
            var lambda = Expression.Lambda<Func<IReader, T>>(body, readerParameter);
            Deserializer<T>._readExpression = lambda;
        }

        private static Expression CreateReadExpressionBody(ParameterExpression readerParameter)
        {
            // first check if there is a direct read method for this type
            var method = GetReadMethod(typeof(T));
            if (method != null)
            {
                var methodCall = method.Call(readerParameter);
                return methodCall;
            }

            // for the none type, just do nothing!
            if (typeof(T) == typeof(None))
            {
                return Expression.Constant(None.Instance);
            }

            // for collections, we call BeginReadingCollection and then read each element
            var enumerableElementType = typeof(T).GetGenericArguments(typeof(IEnumerable<>)).SingleOrDefault();
            if (enumerableElementType != null)
            {
                // create an expression for reading the collection header (basically, the count)
                var beginReadingCollection = Helpers.Method<IReader>(r => r.BeginReadingCollection())
                    .Call(readerParameter);

                // create expressions for initializing a collection instance and adding to it
                Func<Expression, Expression> makeCreateExpression;
                Func<Expression, Expression, Expression, Expression> makeAddExpression;
                CreateBuilderAndSetterForCollectionType(typeof(T), out makeCreateExpression, out makeAddExpression);

                // create an expression for reading in 1 element of the collection
                var readElementExpression = SerializationHelpers.GetDeserializerExpression(enumerableElementType);

                // create an expression for looping over all elements in the collection and reading them into coll
                var sizeExpression = Expression.Parameter(typeof(int), "size");
                var collectionExpression = Expression.Parameter(typeof(T), "collection");
                var readAllElementsExpression = Expression.Block(
                    new[] { sizeExpression, collectionExpression },
                    Expression.Assign(sizeExpression, beginReadingCollection),
                    Expression.Assign(collectionExpression, makeCreateExpression(sizeExpression)),
                    ExpressionHelpers.ForLoop(
                        stopAt: sizeExpression,
                        bodyFactory: (index, label) => makeAddExpression(
                            collectionExpression,
                            Expression.Invoke(readElementExpression, readerParameter),
                            index
                        )
                    ),
                    // putting this as the last element of the block "returns" it
                    collectionExpression
                );
                return readAllElementsExpression;
            }

            // read in all properties IN ORDER and store them in local vars
            var props = SerializationHelpers.GetPropertiesToSerialize(typeof(T));
            var readPropertyExpressions =
                (from pi in props
                let readPropertyExpression = SerializationHelpers.GetDeserializerExpression(pi.PropertyType)
                let param = Expression.Parameter(pi.PropertyType, pi.Name.ToLowerInvariant())
                let assign = Expression.Assign(param, Expression.Invoke(readPropertyExpression, readerParameter))
                select new
                {
                    Property = pi,
                    Variable = param,
                    Assignment = assign
                })
                .ToList();

            // determine the constructor to use
            IDictionary<ParameterInfo, PropertyInfo> mapping;
            var constructor = SerializationHelpers.GetSerializationConstructor(typeof(T), out mapping);
            
            // create the read expression
            var readExpression = Expression.Block(
                readPropertyExpressions.Select(t => t.Variable),
                readPropertyExpressions.Select(t => t.Assignment)
                    .Concat(new Expression[] {
                        // because the value of a block is the last expression, we can just dump the main deserialization
                        // expression here
                        constructor.GetParameters().Any()
                            // for constructors with parameters, we use mapping to assign a variable value
                            // to each parameter
                            ? Expression.New(
                                constructor,
                                arguments: constructor.GetParameters()
                                    .Select(p => readPropertyExpressions.Single(t => t.Property == mapping[p]).Variable)
                            )
                            // for default constructors, we do a memberinit (new Model() { A = a, B = b, ... })
                            : (Expression)Expression.MemberInit(
                                newExpression: Expression.New(typeof(T)),
                                bindings: readPropertyExpressions.Select(t => Expression.Bind(t.Property, t.Variable))
                            )
                    })
            );
            return readExpression;
       }

        private static MethodInfo GetReadMethod(Type type)
        {
            return typeof(IReader).GetMethods()
                .SingleOrDefault(m => m.Name.StartsWith("Read") && m.ReturnType == type);
        }

        private static void CreateBuilderAndSetterForCollectionType(Type requiredType, out Func<Expression, Expression> makeCreateExpression, out Func<Expression, Expression, Expression, Expression> makeAddExpression)
        {
            var elementType = requiredType.GetGenericArguments(typeof(IEnumerable<>)).SingleOrDefault();
            Throw.If(elementType == null, "An IEnumerable type is required!");

            // the case of T[] is special
            if (requiredType.IsArray)
            {
                makeCreateExpression = size => Expression.NewArrayBounds(elementType, size);
                makeAddExpression = (array, element, index) => Expression.Assign(Expression.ArrayAccess(array, index), element);
                return;
            }

            // get the type definition to use
            var typeDef = requiredType.GetGenericTypeDefinition();
            Type typeDefToUse;
            if (typeDef.IsInterface)
            {
                if (typeDef == typeof(IEnumerable<>) || typeDef == typeof(ICollection<>) || typeDef == typeof(IList<>))
                {
                    typeDefToUse = typeof(List<>);
                }
                else if (typeDef == typeof(IDictionary<,>))
                {
                    typeDefToUse = typeof(Dictionary<,>);
                }
                else if (typeDef == typeof(ISet<>))
                {
                    typeDefToUse = typeof(HashSet<>);
                }
                else
                {
                    throw Throw.ShouldNeverGetHere("Unsupported collection interface " + typeDef.Name);
                }
            }
            else
            {
                typeDefToUse = typeDef;
            }

            // create a generic type from the definition
            Type typeToUse;
            if (typeDefToUse.IsGenericOfType(typeof(IDictionary<,>)))
            {
                var kvpTypes = elementType.GetGenericArguments(typeof(KeyValuePair<,>));
                typeToUse = typeDefToUse.MakeGenericType(kvpTypes);
            }
            else
            {
                typeToUse = typeDefToUse.MakeGenericType(elementType);
            }
            Throw.If(typeToUse.IsAbstract, "Abstract collection type properties are not supported!");            

            // determine which constructor to use for creation (we prefer a capacity constructor if there is one)
            var constructorToUse = typeToUse.GetConstructors()
                .Select(c => new { Constructor = c, Parameters = c.GetParameters() })
                .Where(t => !t.Parameters.Any()
                    || (t.Parameters.Length == 1 && t.Parameters[0].ParameterType == typeof(int) && t.Parameters[0].Name == "capacity"))
                .OrderByDescending(t => t.Parameters.Length)
                .FirstOrDefault();
            Throw.InvalidIf(constructorToUse == null, "Could not find a default or capacity constructor!");

            // create the two expressions
            makeCreateExpression = size => Expression.New(constructorToUse.Constructor, constructorToUse.Parameters.Any() ? new[] { size } : Enumerable.Empty<Expression>());
            
            // the add method itself is not actually generic, so our type inference doesn't support it yet
            var addMethod = typeof(ICollection<>).MakeGenericType(elementType)
                .GetMethod("Add");
            makeAddExpression = (collection, element, index) => Expression.Call(
                instance: collection,
                method: addMethod,
                arguments: element
            );
        }
    }
}
