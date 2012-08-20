using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Linq.Expressions;

namespace LinqToHadoop.MapReduce
{
    public class Serialization
    {
        public class Config
        {
            public char KeyValueSeparator { get; private set; }
            public Encoding Encoding { get; private set; }
            public SerializationType SerializationType { get; private set; }
            public bool SkipKey { get; private set; }
            public bool SkipValue { get; private set; }
        }

        [Flags]
        public enum SerializationType
        {
            TextOutput = 1,
            TextInput = (int)TextOutput << 1,
            TextKeyInput = (int)TextInput << 1,
            TextKeyOutput = (int)TextKeyInput << 1,
        }

        [Serializable]
        [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Struct)]
        public class WritableAttribute : Attribute 
        {
            public WritableAttribute(Type serializerType, SerializationType supportedSerializationTypes) 
            {
                this.SerializerType = serializerType;
                this.SupportedSerializationTypes = supportedSerializationTypes;               
            }

            public Type SerializerType { get; private set; }
            public SerializationType SupportedSerializationTypes { get; private set; }
        }

        [Serializable]
        [AttributeUsage(validOn: AttributeTargets.Property)]
        public class OrderAttribute : Attribute 
        {
            public OrderAttribute(int order) 
            {
                this.Order = order;
            }

            public int Order { get; private set; }
        }

        public interface ISerializer<T>
        {
            string ToText(T value, Config config);
            T FromText(string value, Config config);
            string ToKeyText(T value, Config config);
            T FromKeyText(string value, Config config);
        }

        public Action<TKey, TValue, StreamWriter> SerializerFor<TKey, TValue>(Config config)
        {
            var keySerializer = KeySerializerFor<TKey>(config);
            var valueSerializer = ValueSerializerFor<TValue>(config);
            return (key, value, sw) =>
            {
                keySerializer(key, sw);
                sw.Write(config.KeyValueSeparator);
                valueSerializer(value, sw);
            };
        }

        public Action<TKey, StreamWriter> KeySerializerFor<TKey>(Config config)
        {
            var serializer = GetSerializer<TKey>(SerializationType.TextKeyOutput);
            if (serializer != null)
            {
                return (key, stream) => stream.Write(serializer.ToKeyText(key, config));
            }
            return null;
        }

        public Action<TValue, StreamWriter> ValueSerializerFor<TValue>(Config config)
        {
            return null;
        }

        internal ISerializer<T> GetSerializer<T>(SerializationType serializationType)
        {
            var writableAttribute = typeof(T).GetCustomAttributes(typeof(WritableAttribute), inherit: true)
                .OfType<WritableAttribute>()
                .FirstOrDefault();
            if (writableAttribute != null && writableAttribute.SupportedSerializationTypes.HasFlag(serializationType))
            {
                var serializer = (ISerializer<T>)Activator.CreateInstance(writableAttribute.SerializerType);
                return serializer;
            }

            return null;
        }

        //internal Expression<Action<Config, StreamWriter, T>> SerializerExpression<T>()
        //{
        //    if (typeof(T) == typeof(int))
        //    {
        //        return (c, w, t) => new BinaryWriter(w.BaseStream).Write((t as int?).Value);
        //    }
        //}
    }
}
