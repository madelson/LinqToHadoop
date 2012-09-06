using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Concurrent;

namespace LinqToHadoop.Reflection
{
    // TODOS:
    // support inference by instance, return type, not just parameters
    // find and try all inference paths, not just the shortest ones
    // use GetGenericArguments(type<>) throughout
    // full array support
    public static class TypeInference
    {
        private static readonly ConcurrentDictionary<MethodInfo, InferenceStrategy> InferenceStrategyCache = new ConcurrentDictionary<MethodInfo, InferenceStrategy>();

        public static MethodInfo MakeGenericMethodFromParameters(this MethodInfo @this, IList<Type> argumentTypes)
        {
            var strategy = InferenceStrategyCache.GetOrAdd(@this, InferenceStrategy.Derive);
            var method = strategy.MakeGenericMethod(argumentTypes);

            return method;
        }

        internal class InferenceStrategy
        {
            public IList<IList<int>> InferencePaths { get; private set; }
            public MethodInfo Method { get; private set; }

            public MethodInfo MakeGenericMethod(IList<Type> argumentTypes)
            {
                Throw.If(argumentTypes.Count != this.Method.GetParameters().Length, argumentTypes.Count + " parameters supplied to " + this.Method);
                // TODO check that the types are valid for the method

                var genericParameters = this.InferencePaths.Select(p => this.InferGenericParameter(p, argumentTypes));
                var genericMethod = this.Method.MakeGenericMethod(genericParameters.ToArray());
                return genericMethod;
            }

            private Type InferGenericParameter(IList<int> inferencePath, IList<Type> argumentTypes)
            {
                Type typeToExamine = argumentTypes[inferencePath[0]];
                foreach (var i in inferencePath.Skip(1))
                {
                    // TODO this should probably use GetGenericArguments(typedef)
                    typeToExamine = GetGenericOrArrayArguments(typeToExamine)[i];
                }

                return typeToExamine;
            }

            public static InferenceStrategy Derive(MethodInfo method)
            {
                Throw.If(!method.IsGenericMethod, "Method must be generic!");
                
                var genericDefinition = method.GetGenericMethodDefinition();
                var genericArguments = genericDefinition.GetGenericArguments();
                var parameterTypes = genericDefinition.GetParameters().Select(p => p.ParameterType).ToList();
                var inferencePaths = new List<IList<int>>(Enumerable.Repeat<IList<int>>(null, genericArguments.Length));

                // for each parameter type, search for inference paths
                var currentPath = new Stack<int>();
                for (var i = 0; i < parameterTypes.Count; ++i)
                {
                    currentPath.Push(i);
                    DeriveAllInferencePaths(parameterTypes[i], currentPath, inferencePaths, genericArguments);
                    currentPath.Pop();
                }

                return new InferenceStrategy
                {
                    Method = genericDefinition,
                    InferencePaths = inferencePaths.AsReadOnly()
                };
            }

            private static void DeriveAllInferencePaths(Type parameterType, Stack<int> currentPath, IList<IList<int>> inferencePaths, IList<Type> genericArguments)
            {
                var index = genericArguments.IndexOf(parameterType);
                if (index >= 0 && (inferencePaths[index] == null || currentPath.Count < inferencePaths[index].Count))
                {
                    inferencePaths[index] = currentPath.Reverse().ToList().AsReadOnly();
                }

                // recurse
                if (parameterType.IsGenericType)
                {
                    var parameterGenericArguments = GetGenericOrArrayArguments(parameterType);
                    for (var i = 0; i < parameterGenericArguments.Length; ++i)
                    {
                        currentPath.Push(i);
                        DeriveAllInferencePaths(parameterGenericArguments[i], currentPath, inferencePaths, genericArguments);
                        currentPath.Pop();
                    }
                }
            }

            private static Type[] GetGenericOrArrayArguments(Type type)
            {
                return type.IsArray
                    ? new[] { type.GetElementType() }
                    : type.GetGenericArguments();
            }
        }
    }
}
