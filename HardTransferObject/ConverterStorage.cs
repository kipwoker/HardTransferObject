using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace HardTransferObject
{
    public class ConverterStorage
    {
        public static readonly ConverterStorage Instance = new ConverterStorage();
        public static readonly FieldInfo InstanceFieldInfo = typeof(ConverterStorage).GetField(nameof(Instance), BindingFlags.Public | BindingFlags.Static);
        public static readonly MethodInfo GetImplementationFieldInfo = typeof(ConverterStorage).GetMethod(nameof(GetImplementation), BindingFlags.Public | BindingFlags.Instance);

        private readonly ConcurrentDictionary<string, IConverter<object, object>> converterImplementationMap = new ConcurrentDictionary<string, IConverter<object, object>>();
        private readonly ConcurrentDictionary<string, Type> converterTypeMap = new ConcurrentDictionary<string, Type>();

        public void Add(Type inType, Type outType, Type converterType)
        {
            var key = GetKey(inType, outType);
            converterTypeMap.AddOrUpdate(key, converterType, (i, c) => c);
            converterImplementationMap.AddOrUpdate(key, (IConverter<object, object>)Activator.CreateInstance(converterType), (i, c) => c);
        }

        public bool Contains(Type inType, Type outType)
        {
            return converterTypeMap.ContainsKey(GetKey(inType, outType));
        }

        public IConverter<object, object> GetImplementation(Type inType, Type outType)
        {
            return converterImplementationMap[GetKey(inType, outType)];
        }

        public Type GetType(Type inType, Type outType)
        {
            return converterTypeMap[GetKey(inType, outType)];
        }

        private static string GetKey(Type inType, Type outType)
        {
            return inType.FullName + outType.FullName;
        }
    }
}