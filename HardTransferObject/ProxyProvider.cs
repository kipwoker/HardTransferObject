using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace HardTransferObject
{
    public class ProxyProvider : IProxyProvider
    {
        private readonly ModuleBuilder moduleBuilder;

        private readonly Dictionary<Type, ProxyMapping> proxyMap = new Dictionary<Type, ProxyMapping>();
        private readonly Dictionary<Type, Type> genericImplementationsMap = new Dictionary<Type, Type>();
        private readonly ConverterBuilder converterBuilder;

        public ProxyProvider(
            ModuleBuilder moduleBuilder)
        {
            this.moduleBuilder = moduleBuilder;
            converterBuilder = new ConverterBuilder(moduleBuilder);
        }

        public void Declare(Type baseType)
        {
            GetOrCreate(baseType);
        }

        public ProxyMapping GetMapping(Type baseType)
        {
            return proxyMap[baseType];
        }

        public Dictionary<Type, Type> TypeMap => proxyMap.ToDictionary(x => x.Key, x => x.Value.ProxyType);

        public ProxyMapping[] GetMappingChain(Type baseType)
        {
            var list = new List<ProxyMapping>();

            var typeIterator = baseType;
            while (true)
            {
                if (!proxyMap.ContainsKey(typeIterator))
                {
                    return list.ToArray();
                }

                var proxyMapping = proxyMap[typeIterator];
                list.Add(proxyMapping);
                typeIterator = proxyMapping.ProxyType;
            }
        }

        private Type GetOrCreate(Type baseType)
        {
            if (proxyMap.ContainsKey(baseType))
            {
                return proxyMap[baseType].ProxyType;
            }

            var proxyType = CreateWithoutCache(baseType);
            TryCreateProxyMapping(baseType, proxyType);
            return proxyType;
        }

        private void TryCreateProxyMapping(Type baseType, Type proxyType)
        {
            if (baseType == proxyType || proxyMap.ContainsKey(baseType))
            {
                return;
            }

            var proxyMapping = new ProxyMapping(
                proxyType,
                converterBuilder.Build(baseType, proxyType).Convert,
                converterBuilder.Build(proxyType, baseType).Convert);

            proxyMap[baseType] = proxyMapping;
            GetOrCreate(proxyType);
        }

        private Type CreateWithoutCache(Type baseType)
        {
            if (baseType == typeof(string))
            {
                return baseType;
            }

            if (IsNullable(baseType))
            {
                return baseType;
            }

            if (baseType.IsArray)
            {
                return CreateArrayImplementation(baseType);
            }

            if (!baseType.IsInterface && !baseType.IsGenericType)
            {
                return baseType;
            }

            if (IsIEnumerableInterface(baseType))
            {
                return CreateIEnumerableImplementation(baseType);
            }

            if (baseType.IsGenericType)
            {
                return CreateGenericImplementation(baseType);
            }

            if (baseType.IsInterface)
            {
                return CreateImplementation(baseType);
            }

            throw new Exception($"Unknown type {baseType}");
        }

        private static bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        private Type CreateArrayImplementation(Type type)
        {
            var originalElementType = type.GetElementType();
            var patchedElementType = GetOrCreate(originalElementType);
            if (originalElementType == patchedElementType)
            {
                return type;
            }

            return patchedElementType.MakeArrayType();
        }

        private Type CreateGenericImplementation(Type type)
        {
            if (type.IsInterface)
            {
                var implementationType = CreateImplementation(type);
                TryCreateProxyMapping(type, implementationType);
                TryCreateProxyMapping(implementationType, GetOrCreate(implementationType));

                return implementationType;
            }

            var originalGenericArguments = type.GetGenericArguments();
            var patchedGenericArguments = originalGenericArguments.Select(GetOrCreate).ToArray();
            var areArgsEqual = !originalGenericArguments.Where((x, i) => x != patchedGenericArguments[i]).Any();

            if (!areArgsEqual)
            {
                var patchedArgsType = type
                    .GetGenericTypeDefinition()
                    .MakeGenericType(patchedGenericArguments);
                var implementationType = CreateImplementation(patchedArgsType);
                TryCreateProxyMapping(type, implementationType);
                return implementationType;
            }

            return type;
        }

        private Type CreateIEnumerableImplementation(Type type)
        {
            var enumerableType = GetEnumerableType(type);
            var patchedType = GetOrCreate(enumerableType).MakeArrayType();
            return patchedType;
        }

        private static Type GetEnumerableType(Type type)
        {
            if (IsIEnumerableInterface(type))
            {
                return type.GetGenericArguments()[0];
            }

            return type
                .GetInterfaces()
                .FirstOrDefault(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))?
                .GetGenericArguments()[0];
        }

        private static bool IsIEnumerableInterface(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        private Type CreateImplementation(Type type)
        {
            //todo: if type has methods should throw exception

            if (type.IsGenericType && genericImplementationsMap.ContainsKey(type))
            {
                return genericImplementationsMap[type].MakeGenericType(type.GenericTypeArguments);
            }

            var className = type.GetTypeName();
            var classBuilder = moduleBuilder.DefineType(className, TypeAttributes.Class | TypeAttributes.Public);

            if (type.IsGenericType)
            {
                var genericTypeArguments = type.GenericTypeArguments;
                var builders = classBuilder.DefineGenericParameters(genericTypeArguments.Select((x, i) => $"T{i}").ToArray());
                //todo: add constraints
            }

            if (type.IsInterface)
            {
                classBuilder.AddInterfaceImplementation(type);
            }

            foreach (var property in type.GetProperties())
            {
                Type propType;
                if (type.IsInterface || IsGenericArgTypeProperty(type, property))
                {
                    propType = property.PropertyType;
                }
                else
                {
                    propType = GetOrCreate(property.PropertyType);
                }

                GenerateClassProperty(classBuilder, property.Name, propType);
            }

            var implementationType = classBuilder.CreateType();

            if (type.IsGenericType)
            {
                genericImplementationsMap[type] = implementationType;
                return implementationType.MakeGenericType(type.GenericTypeArguments);
            }

            return implementationType;
        }

        private static bool IsGenericArgTypeProperty(Type type, PropertyInfo property)
        {
            return type.IsGenericType && type.GenericTypeArguments.Contains(property.PropertyType);
        }

        private static void GenerateClassProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            const MethodAttributes getAttr = MethodAttributes.Public |
                                             MethodAttributes.Virtual |
                                             MethodAttributes.NewSlot |
                                             MethodAttributes.HideBySig;
            const MethodAttributes setAttr = MethodAttributes.Public |
                                             MethodAttributes.Virtual |
                                             MethodAttributes.NewSlot |
                                             MethodAttributes.HideBySig;

            // Generate a private field
            var field = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            // Generate a public property
            var property = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, new[] { propertyType });

            // Define the "get" accessor method for current private field.
            var getMethodBuilder = typeBuilder.DefineMethod("get_" + propertyName, getAttr, propertyType, Type.EmptyTypes);

            var ilGet = getMethodBuilder.GetILGenerator();
            ilGet.Ldarg(0);
            ilGet.Ldfld(field);
            ilGet.Ret();

            // Define the "set" accessor method for current private field.
            var setMethodBuilder = typeBuilder.DefineMethod("set_" + propertyName, setAttr, null, new[] { propertyType });

            var ilSet = setMethodBuilder.GetILGenerator();
            ilSet.Ldarg(0);
            ilSet.Ldarg(1);
            ilSet.Stfld(field);
            ilSet.Ret();

            //map property methods
            property.SetGetMethod(getMethodBuilder);
            property.SetSetMethod(setMethodBuilder);
        }
    }
}