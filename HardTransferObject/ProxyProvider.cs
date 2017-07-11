using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace HardTransferObject
{
    public static class Converters
    {
        public static object Convert<T, TCollection>(T[] array)
            where TCollection : IEnumerable<T>
        {
            if (typeof(TCollection) == typeof(List<T>))
            {
                return new List<T>(array);
            }

            return null;
        }
    }


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
                BuildConverter(baseType, proxyType).Convert,
                BuildConverter(proxyType, baseType).Convert);

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
            //todo: if interface has methods should throw exception

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
                if ((type.IsGenericType && type.GenericTypeArguments.Contains(property.PropertyType)) || type.IsInterface)
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

        private IConverter<object, object> BuildConverter(Type inType, Type outType)
        {
            var converter = converterBuilder.Build(inType, outType);
            return converter;
        }
    }

    public class ConverterBuilder
    {
        private readonly IConverter<object, object> idealConverter = new IdealConverter();
        private static readonly MethodInfo typeOfMethodInfo = typeof(Type)
            .GetMethod(nameof(Type.GetTypeFromHandle), BindingFlags.Public | BindingFlags.Static);

        private static readonly MethodInfo convertMethodInfo = typeof(IConverter<object, object>)
            .GetMethod(nameof(IConverter<object, object>.Convert), BindingFlags.Public | BindingFlags.Instance);

        private readonly Type objectType = typeof(object);

        private readonly ModuleBuilder moduleBuilder;

        public ConverterBuilder(ModuleBuilder moduleBuilder)
        {
            this.moduleBuilder = moduleBuilder;
        }

        public IConverter<object, object> Build(Type inType, Type outType)
        {
            if (inType == outType)
            {
                return idealConverter;
            }

            if (!ConverterStorage.Instance.Contains(inType, outType))
            {
                Create(inType, outType);
            }

            return ConverterStorage.Instance.GetImplementation(inType, outType);
        }

        private void Create(Type inType, Type outType)
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
                objectType,
                new[] { objectType });

            //if (outType.IsInterface)
            //{
            //    Console.WriteLine($"I : {inType.Name} -> {outType.Name}");
            //    var converter = CreateToInterfaceConverter(converterBuilder, methodBuilder, inType, outType);
            //    ConverterStorage.Instance.Add(inType, outType, converter);
            //    return;
            //}

            var interfaceToObjectConverterType = CreateToObjectConverter(converterBuilder, methodBuilder, inType, outType);
            ConverterStorage.Instance.Add(inType, outType, interfaceToObjectConverterType);
        }

        

        private Type CreateToInterfaceConverter(TypeBuilder converterBuilder, MethodBuilder methodBuilder, Type inType, Type outType)
        {
            var ilConvert = methodBuilder.GetILGenerator();

            var v0 = ilConvert.DeclareLocal(objectType);
            var brLabel = ilConvert.DefineLabel();

            ilConvert.Ldarg(1);
            ilConvert.Castclass(outType);
            ilConvert.Stloc(v0);
            ilConvert.BrS(brLabel);
            ilConvert.MarkLabel(brLabel);
            ilConvert.Ldloc(v0);
            ilConvert.Ret();

            return converterBuilder.CreateType();
        }

        private Type CreateToObjectConverter(TypeBuilder converterBuilder, MethodBuilder methodBuilder, Type inType, Type outType)
        {
            var ilConvert = methodBuilder.GetILGenerator();

            var outProps = outType.GetProperties();
            var inProps = inType.GetProperties();

            var casted = ilConvert.DeclareLocal(inType);
            var converted = ilConvert.DeclareLocal(outType);
            var v2 = ilConvert.DeclareLocal(outType);
            var v3 = ilConvert.DeclareLocal(objectType);
            var brLabel = ilConvert.DefineLabel();
            var returnType = !outType.IsInterface ? outType : inType;

            //todo: for debug
            //ilConvert.Ldstr("{0} to {1}");
            //ilConvert.Ldarg(1);
            //ilConvert.Callvirt(typeof(object).GetMethod("GetType"));
            //ilConvert.Ldtoken(returnType);
            //ilConvert.Call(typeOfMethodInfo);
            //ilConvert.Call(typeof(string).GetMethod("Format",new []{ typeof(string), typeof(object), typeof(object) }));
            //ilConvert.Call(typeof(Console).GetMethod("WriteLine", new []{ typeof(string) }));

            //var casted = (IModel1<...>) @in;
            ilConvert.Ldarg(1);
            ilConvert.Castclass(inType);
            ilConvert.Stloc(casted);

            //return new Model1<...>
            var returnConstructor = returnType.GetConstructor(Type.EmptyTypes);
            ilConvert.Newobj(returnConstructor);
            ilConvert.Stloc(v2);

            for (var i = 0; i < outProps.Length; i++)
            {
                var inProp = inProps[i];
                var outProp = outProps[i];

                if (!outType.IsInterface && inProp.PropertyType != outProp.PropertyType)
                {
                    //OutProp = (OutPropType)ConverterStorage.Instance.GetImplementation(casted.InProp.GetType(), casted.OutProp.GetType()).Convert(casted.InProp)
                    ilConvert.Ldloc(v2);
                    ilConvert.Ldsfld(ConverterStorage.InstanceFieldInfo);
                    ilConvert.Ldtoken(inProp.PropertyType);
                    ilConvert.Call(typeOfMethodInfo);
                    ilConvert.Ldtoken(outProp.PropertyType);
                    ilConvert.Call(typeOfMethodInfo);
                    ilConvert.Callvirt(ConverterStorage.GetImplementationFieldInfo);
                    ilConvert.Ldloc(casted);
                    ilConvert.Callvirt(inProp.GetMethod);
                    ilConvert.Callvirt(convertMethodInfo);
                    ilConvert.Castclass(outProp.PropertyType);
                    ilConvert.Callvirt(outProp.SetMethod);

                    continue;
                }

                //Prop1 = casted.Prop1,
                ilConvert.Ldloc(v2);
                ilConvert.Ldloc(casted);
                ilConvert.Callvirt(inProp.GetMethod);
                ilConvert.Callvirt(!outType.IsInterface ? outProp.SetMethod : inProp.SetMethod);
            }

            ilConvert.Ldloc(v2);
            ilConvert.Stloc(converted);
            ilConvert.Ldloc(converted);
            ilConvert.Stloc(v3);
            ilConvert.BrS(brLabel);
            ilConvert.MarkLabel(brLabel);
            ilConvert.Ldloc(v3);
            ilConvert.Ret();

            return converterBuilder.CreateType();
        }
    }

    public static class TypeExtensions
    {
        private static int iterator = 0;
        private static readonly Dictionary<Type, string> nameMap = new Dictionary<Type, string>();

        public static string GetTypeName(this Type type)
        {
            if (!nameMap.ContainsKey(type))
            {
                nameMap[type] = $"C_{iterator}";
                ++iterator;
            }
            
            return nameMap[type];
        }

        public static Dictionary<Type, string> GetAll()
        {
            return nameMap;
        }
    }

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