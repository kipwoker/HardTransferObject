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
        private readonly IConverter<object, object> idealConverter = new IdealConverter();

        public ProxyProvider(
            ModuleBuilder moduleBuilder)
        {
            this.moduleBuilder = moduleBuilder;
        }

        public void Add(Type baseType)
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

            var proxyType = GetOrCreateWithoutCache(baseType);
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
                CreateConverter(baseType, proxyType).Convert,
                CreateConverter(proxyType, baseType).Convert);

            proxyMap[baseType] = proxyMapping;
            GetOrCreate(proxyType);
        }

        private Type GetOrCreateWithoutCache(Type baseType)
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
                return CreateInterfaceImplementation(baseType);
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
            var originalGenericArguments = type.GetGenericArguments();
            var patchedGenericArguments = originalGenericArguments.Select(GetOrCreate).ToArray();

            if (type.IsInterface)
            {
                var implementationType = CreateInterfaceImplementation(type);
                TryCreateProxyMapping(type, implementationType);

                return implementationType;
            }

            if (originalGenericArguments.Where((x, i) => x != patchedGenericArguments[i]).Any())
            {
                var patchedArgsType = type
                    .GetGenericTypeDefinition()
                    .MakeGenericType(patchedGenericArguments);
                TryCreateProxyMapping(type, patchedArgsType);
                return patchedArgsType;
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

        private Type CreateInterfaceImplementation(Type type)
        {
            //todo: if interface has methods should throw exception

            if (type.IsGenericType && genericImplementationsMap.ContainsKey(type))
            {
                return genericImplementationsMap[type].MakeGenericType(type.GenericTypeArguments);
            }

            var className = $"C_{type.Name.TrimStart('I')}";
            var classBuilder = moduleBuilder.DefineType(className, TypeAttributes.Class | TypeAttributes.Public);

            if (type.IsGenericType)
            {
                var genericTypeArguments = type.GenericTypeArguments;
                var builders = classBuilder.DefineGenericParameters(genericTypeArguments.Select((x, i) => $"T{i}").ToArray());
                //todo: add constraints
            }

            classBuilder.AddInterfaceImplementation(type);

            foreach (var property in type.GetProperties())
            {
                GenerateClassProperty(classBuilder, property.Name, property.PropertyType);
            }

            var implementationType = classBuilder.CreateType();

            if (type.IsGenericType)
            {
                genericImplementationsMap[type] = implementationType;
                return implementationType.MakeGenericType(type.GenericTypeArguments);
            }

            return implementationType;
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

        private readonly Dictionary<Type, IConverter<object, object>> converterMap = new Dictionary<Type, IConverter<object, object>>();

        private IConverter<object, object> CreateConverter(Type inType, Type outType)
        {
            if (inType == outType)
            {
                return idealConverter;
            }

            if (converterMap.ContainsKey(inType))
            {
                return converterMap[inType];
            }

            if (inType.IsInterface)
            {
                var interfaceToObjectConverter = CreateInterfaceToObjectConverter(inType, outType);
                converterMap[inType] = interfaceToObjectConverter;
                return interfaceToObjectConverter;
            }

            if (outType.IsInterface)
            {
                var objectToInterfaceConverter = CreateObjectToInterfaceConverter(inType, outType);
                converterMap[inType] = objectToInterfaceConverter;
                return objectToInterfaceConverter;
            }

            throw new NotImplementedException($"{inType} -> {outType}");
        }

        private IConverter<object, object> CreateInterfaceToObjectConverter(Type inType, Type outType)
        {
            var converterName = $"{inType.Name}_To_{outType.Name}Converter";
            var converterBuilder = moduleBuilder.DefineType(converterName, TypeAttributes.Class | TypeAttributes.Public);
            converterBuilder.AddInterfaceImplementation(typeof(IConverter<object, object>));

            var methodBuilder = converterBuilder.DefineMethod(
                nameof(IConverter<object, object>.Convert),
                MethodAttributes.Public |
                MethodAttributes.Final |
                MethodAttributes.HideBySig |
                MethodAttributes.Virtual,
                typeof(object),
                new[] {typeof(object)});

            var ilConvert = methodBuilder.GetILGenerator();
            var casted = ilConvert.DeclareLocal(inType);
            var v1 = ilConvert.DeclareLocal(typeof(object));

            //var casted = (IModel1<string>) @in;
            ilConvert.Ldarg(1);
            ilConvert.Castclass(inType);
            ilConvert.Stloc(casted);

            ilConvert.Newobj(outType.GetConstructor(Type.EmptyTypes));

            var outProps = outType.GetProperties();
            var inProps = inType.GetProperties();
            for (var i = 0; i < outProps.Length; i++)
            {
                ilConvert.Dup();
                ilConvert.Ldloc(casted);
                //ilConvert.Callvirt();
            }

            /*
            return new outType {
                Prop1 = ConverterMap[in.Prop1.Type].Convert(in.Prop1)
            };
            */

            throw new NotImplementedException($"{inType} -> {outType}");
        }

        private IConverter<object, object> CreateObjectToInterfaceConverter(Type inType, Type outType)
        {
            /*
            return in;
            */

            throw new NotImplementedException($"{inType} -> {outType}");
        }
    }
}