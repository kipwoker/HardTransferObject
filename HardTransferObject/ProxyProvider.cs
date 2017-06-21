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

        private readonly IProxyConverterFactory proxyConverterFactory;

        public ProxyProvider(
            ModuleBuilder moduleBuilder,
            IProxyConverterFactory proxyConverterFactory)
        {
            this.moduleBuilder = moduleBuilder;
            this.proxyConverterFactory = proxyConverterFactory;
        }

        private readonly Dictionary<Type, ProxyMapping> proxyMap = new Dictionary<Type, ProxyMapping>();

        public Dictionary<Type, Type> SelectAll()
        {
            return proxyMap.ToDictionary(x => x.Key, x => x.Value.ProxyType);
        }

        public ProxyMapping GetOrCreate(Type baseType)
        {
            if (baseType == typeof(string))
            {
                return CreateProxyMapping(baseType, baseType);
            }

            if (IsNullable(baseType))
            {
                return CreateProxyMapping(baseType, baseType);
            }

            if (baseType.IsArray)
            {
                return CreateArrayImplementation(baseType);
            }

            if (!baseType.IsInterface && !baseType.IsGenericType)
            {
                return CreateProxyMapping(baseType, baseType);
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
                return CreateInterfaceImplementation(baseType);
            }

            throw new Exception($"Unknown type {baseType}");
        }

        private ProxyMapping CreateProxyMapping(Type baseType, Type proxyType)
        {
            if (proxyMap.ContainsKey(baseType))
            {
                return proxyMap[baseType];
            }

            var proxyMapping = new ProxyMapping(
                proxyType,
                proxyConverterFactory.CreateConverterToProxy(baseType, proxyType).Convert,
                proxyConverterFactory.CreateConverterToBase(baseType, proxyType).Convert);

            if (baseType.IsInterface && proxyType.IsInterface)
            {
                return proxyMapping;
            }

            proxyMap[baseType] = proxyMapping;
            return proxyMapping;
        }

        private Type GetOrCreateProxyType(Type baseType)
        {
            return GetOrCreate(baseType).ProxyType;
        }

        private static bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        private ProxyMapping CreateArrayImplementation(Type type)
        {
            var originalElementType = type.GetElementType();
            var patchedElementType = GetOrCreateProxyType(originalElementType);
            if (originalElementType == patchedElementType)
            {
                return CreateProxyMapping(type, type);
            }

            return CreateProxyMapping(type, patchedElementType.MakeArrayType());
        }

        private ProxyMapping CreateGenericImplementation(Type type)
        {
            var implementedGenericArgsType = GetOrCreateGenericArgsImplementedType(type);

            if (implementedGenericArgsType.ProxyType.IsInterface)
            {
                return CreateInterfaceImplementation(implementedGenericArgsType.ProxyType);
            }

            return implementedGenericArgsType;
        }

        private ProxyMapping GetOrCreateGenericArgsImplementedType(Type type)
        {
            var originalGenericArguments = type.GetGenericArguments();
            var pathedGenericArguments = originalGenericArguments.Select(GetOrCreateProxyType).ToArray();

            Type patchedType;
            if (!originalGenericArguments.Where((x, i) => x != pathedGenericArguments[i]).Any())
            {
                patchedType = type;
            }
            else
            {
                patchedType = type
                    .GetGenericTypeDefinition()
                    .MakeGenericType(pathedGenericArguments);
            }
            return CreateProxyMapping(type, patchedType);
        }

        private ProxyMapping CreateIEnumerableImplementation(Type type)
        {
            var enumerableType = GetEnumerableType(type);
            var patchedType = GetOrCreateProxyType(enumerableType).MakeArrayType();
            return CreateProxyMapping(type, patchedType);
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

        private ProxyMapping CreateInterfaceImplementation(Type type)
        {
            //todo: if interface has methods should throw exception

            var suffix = string.Empty;
            if (type.IsGenericType)
            {
                suffix = string.Join(string.Empty, type.GetGenericArguments().Select(x => x.Name));
            }

            var className = $"{type.Name.TrimStart('I')}AutoImpl{suffix}";
            var classBuilder = moduleBuilder.DefineType(className, TypeAttributes.Class | TypeAttributes.Public);

            foreach (var property in type.GetProperties())
            {
                GetOrCreateProxyType(property.PropertyType);
                GenerateClassProperty(classBuilder, property.Name, property.PropertyType);
            }

            var implementationType = classBuilder.CreateType();

            return CreateProxyMapping(type, implementationType);
        }

        private static void GenerateClassProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            const MethodAttributes getAttr = MethodAttributes.Public |
                                             MethodAttributes.HideBySig;
            const MethodAttributes setAttr = MethodAttributes.Public |
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