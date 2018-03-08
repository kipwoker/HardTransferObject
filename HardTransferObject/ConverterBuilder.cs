using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace HardTransferObject
{
    public class ConverterBuilder
    {
        private readonly IConverter<object, object> idealConverter = new IdealConverter();
        private static readonly MethodInfo typeOfMethodInfo = typeof(Type)
            .GetMethod(nameof(Type.GetTypeFromHandle), BindingFlags.Public | BindingFlags.Static);

        private static readonly MethodInfo convertMethodInfo = typeof(IConverter<object, object>)
            .GetMethod(nameof(IConverter<object, object>.Convert), BindingFlags.Public | BindingFlags.Instance);

        private readonly Type objectType = typeof(object);
        private readonly Type intType = typeof(int);
        private readonly Type boolType = typeof(bool);

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

            var ilConvertGenerator = methodBuilder.GetILGenerator();

            if (inType.IsArray || outType.IsArray)
            {
                EmitCollectionConverter(ilConvertGenerator, inType, outType);
            }
            else if (outType.IsValueType)
            {
                EmitStructConverter(ilConvertGenerator, inType, outType);
            }
            else
            {
                EmitObjectConverter(ilConvertGenerator, inType, outType);
            }

            ConverterStorage.Instance.Add(inType, outType, converterBuilder.CreateType());
        }

        private void EmitStructConverter(ILGenerator il, Type inType, Type outType)
        {
            var casted = il.DeclareLocal(inType);
            var converted = il.DeclareLocal(outType);
            var v2 = il.DeclareLocal(objectType);

            var inPropMap = inType.GetProperties().ToDictionary(x => x.Name.ToLower());
            var outCtor = outType.GetConstructors().OrderByDescending(x => x.GetParameters().Length).First();
            var ctorParamMap = outCtor
                .GetParameters()
                .Select(outParam =>
                {
                    var outParamKey = outParam.Name.ToLower();
                    return new {outParam, inProp = inPropMap.ContainsKey(outParamKey) ? inPropMap[outParamKey] : null};
                })
                .ToArray();

            var invalidParamTransitions = ctorParamMap.Where(x => x.inProp == null).ToArray();
            if (invalidParamTransitions.Length > 0)
            {
                throw new Exception($"Can't serialize this kind of struct. Need to know how to map next ctor params: {string.Join(", ", invalidParamTransitions.Select(x => x.outParam.Name))}");
            }


            il.Ldarg(1);
            il.Unbox_Any(inType);
            il.Stloc(casted);

            il.Ldloca(converted);

            foreach (var ctorParamPair in ctorParamMap)
            {
                var inItem = ctorParamPair.inProp;
                var outItem = ctorParamPair.outParam;
                var inItemType = inItem.PropertyType;
                var outItemType = outItem.ParameterType;
                if (inItemType == outItemType)
                {
                    il.Ldloca(casted);
                    il.Call(inItem.GetMethod);
                }
                else
                {
                    il.Ldsfld(ConverterStorage.InstanceFieldInfo);
                    il.Ldtoken(inItemType);
                    il.Call(typeOfMethodInfo);
                    il.Ldtoken(outItemType);
                    il.Call(typeOfMethodInfo);
                    il.Callvirt(ConverterStorage.GetImplementationFieldInfo);
                    il.Ldloca(casted);
                    il.Call(inItem.GetMethod);
                    il.Callvirt(convertMethodInfo);
                    il.Castclass(outItemType);
                }
            }

            il.Call(outCtor);

            il.Ldloc(converted);
            il.Box(outType);
            il.Stloc(v2);

            var brLabel = il.DefineLabel();
            il.BrS(brLabel);
            il.MarkLabel(brLabel);

            il.Ldloc(v2);
            il.Ret();
        }

        private void EmitCollectionConverter(ILGenerator il, Type inType, Type outType)
        {
            if ((inType.IsIEnumerableInterface() || inType.IsIEnumerableInterfaceImplementation()) && outType.IsArray)
            {
                var inItemType = inType.GetEnumerableType();
                var outItemType = outType.GetElementType();

                if (inItemType != outItemType)
                {
                    var casted = il.DeclareLocal(inType);
                    var array = il.DeclareLocal(inItemType.MakeArrayType());
                    var converted = il.DeclareLocal(inType);
                    var i = il.DeclareLocal(intType);
                    var v4 = il.DeclareLocal(boolType);
                    var v5 = il.DeclareLocal(objectType);
                    var loopEntryPoint = il.DefineLabel();
                    var continueLoop = il.DefineLabel();

                    il.Ldarga(1);
                    il.Castclass(inType);
                    il.Stloc(casted);

                    il.Ldloc(casted);
                    il.Call(typeof(Enumerable).GetMethod("ToArray").MakeGenericMethod(inItemType));
                    il.Stloc(array);

                    il.Ldloc(array);
                    il.LdLen();
                    il.ConvI4();
                    il.Newarr(outItemType);
                    il.Stloc(converted);

                    il.Ldc_I4(0);
                    il.Stloc(i);

                    il.BrS(loopEntryPoint);
                    
                    il.MarkLabel(continueLoop);

                    il.Ldloc(converted);
                    il.Ldloc(i);
                    il.Ldsfld(ConverterStorage.InstanceFieldInfo);
                    il.Ldtoken(inItemType);
                    il.Call(typeOfMethodInfo);
                    il.Ldtoken(outItemType);
                    il.Call(typeOfMethodInfo);
                    il.Callvirt(ConverterStorage.GetImplementationFieldInfo);
                    il.Ldloc(array);
                    il.Ldloc(i);
                    il.Ldelem_Ref();
                    il.Callvirt(convertMethodInfo);
                    il.Castclass(outType);
                    il.Stelem_Ref();

                    il.Ldloc(i);
                    il.Ldc_I4(1);
                    il.Add();
                    il.Stloc(i);

                    il.MarkLabel(loopEntryPoint);
                    il.Ldloc(i);
                    il.Ldloc(array);
                    il.LdLen();
                    il.ConvI4();
                    il.Clt();
                    il.Stloc(v4);

                    il.Ldloc(v4);
                    il.BrtrueS(continueLoop);

                    il.Ldloc(converted);
                    il.Stloc(v5);

                    var ret = il.DefineLabel();
                    il.BrS(ret);
                    il.MarkLabel(ret);
                    il.Ldloc(v5);
                    il.Ret();
                }
                else
                {
                    var casted = il.DeclareLocal(inType);
                    var array = il.DeclareLocal(inItemType.MakeArrayType());
                    var v2 = il.DeclareLocal(objectType);

                    il.Ldarg(1);
                    il.Castclass(inType);
                    il.Stloc(casted);

                    il.Ldloc(casted);
                    il.Call(typeof(Enumerable).GetMethod("ToArray").MakeGenericMethod(inItemType));
                    il.Stloc(array);

                    il.Ldloc(array);
                    il.Stloc(v2);

                    var ret = il.DefineLabel();
                    il.BrS(ret);
                    il.MarkLabel(ret);
                    il.Ldloc(v2);
                    il.Ret();
                }
            }
            else
            {
                var inItemType = inType.GetElementType();
                var outItemType = outType.GetEnumerableType();

                if (inItemType != outItemType)
                {
                    var casted = il.DeclareLocal(inType);
                    var converted = il.DeclareLocal(inType);
                    var i = il.DeclareLocal(intType);
                    var v3 = il.DeclareLocal(boolType);
                    var v4 = il.DeclareLocal(objectType);
                    var loopEntryPoint = il.DefineLabel();
                    var continueLoop = il.DefineLabel();

                    il.Ldarga(1);
                    il.Castclass(inType);
                    il.Stloc(casted);

                    il.Ldloc(casted);
                    il.LdLen();
                    il.ConvI4();
                    il.Newarr(outItemType);
                    il.Stloc(converted);

                    il.Ldc_I4(0);
                    il.Stloc(i);

                    il.BrS(loopEntryPoint);

                    il.MarkLabel(continueLoop);

                    il.Ldloc(converted);
                    il.Ldloc(i);
                    il.Ldsfld(ConverterStorage.InstanceFieldInfo);
                    il.Ldtoken(inItemType);
                    il.Call(typeOfMethodInfo);
                    il.Ldtoken(outItemType);
                    il.Call(typeOfMethodInfo);
                    il.Callvirt(ConverterStorage.GetImplementationFieldInfo);
                    il.Ldloc(casted);
                    il.Ldloc(i);
                    il.Ldelem_Ref();
                    il.Callvirt(convertMethodInfo);
                    il.Castclass(outType);
                    il.Stelem_Ref();

                    il.Ldloc(i);
                    il.Ldc_I4(1);
                    il.Add();
                    il.Stloc(i);

                    il.MarkLabel(loopEntryPoint);
                    il.Ldloc(i);
                    il.Ldloc(casted);
                    il.LdLen();
                    il.ConvI4();
                    il.Clt();
                    il.Stloc(v3);

                    il.Ldloc(v3);
                    il.BrtrueS(continueLoop);

                    il.Ldloc(converted);
                    il.Stloc(v4);

                    var ret = il.DefineLabel();
                    il.BrS(ret);
                    il.MarkLabel(ret);
                    il.Ldloc(v4);
                    il.Ret();
                }
                else
                {
                    var v0 = il.DeclareLocal(objectType);

                    il.Ldarg(1);
                    il.Castclass(inType);
                    il.Stloc(v0);

                    var ret = il.DefineLabel();
                    il.BrS(ret);
                    il.MarkLabel(ret);
                    il.Ldloc(v0);
                    il.Ret();
                }
            }
        }

        private void EmitObjectConverter(ILGenerator il, Type inType, Type outType)
        {
            var outProps = outType.GetProperties();
            var inProps = inType.GetProperties();

            var casted = il.DeclareLocal(inType);
            var converted = il.DeclareLocal(outType);
            var v2 = il.DeclareLocal(outType);
            var v3 = il.DeclareLocal(objectType);
            var brLabel = il.DefineLabel();
            var returnType = !outType.IsInterface ? outType : inType;

            //var casted = (IModel1<...>) @in;
            il.Ldarg(1);
            il.Castclass(inType);
            il.Stloc(casted);

            //return new Model1<...>
            var returnConstructor = returnType.GetConstructor(Type.EmptyTypes);
            il.Newobj(returnConstructor);
            il.Stloc(v2);

            for (var i = 0; i < outProps.Length; i++)
            {
                var inProp = inProps[i];
                var outProp = outProps[i];

                if (!outType.IsInterface && inProp.PropertyType != outProp.PropertyType)
                {
                    //OutProp = (OutPropType)ConverterStorage.Instance.GetImplementation(casted.InProp.GetType(), casted.OutProp.GetType()).Convert(casted.InProp)
                    il.Ldloc(v2);
                    il.Ldsfld(ConverterStorage.InstanceFieldInfo);
                    il.Ldtoken(inProp.PropertyType);
                    il.Call(typeOfMethodInfo);
                    il.Ldtoken(outProp.PropertyType);
                    il.Call(typeOfMethodInfo);
                    il.Callvirt(ConverterStorage.GetImplementationFieldInfo);
                    il.Ldloc(casted);
                    il.Callvirt(inProp.GetMethod);
                    il.Callvirt(convertMethodInfo);
                    il.Castclass(outProp.PropertyType);
                    il.Callvirt(outProp.SetMethod);

                    continue;
                }

                //Prop1 = casted.Prop1,
                il.Ldloc(v2);
                il.Ldloc(casted);
                il.Callvirt(inProp.GetMethod);
                il.Callvirt(!outType.IsInterface ? outProp.SetMethod : inProp.SetMethod);
            }

            il.Ldloc(v2);
            il.Stloc(converted);
            il.Ldloc(converted);
            il.Stloc(v3);
            il.BrS(brLabel);
            il.MarkLabel(brLabel);
            il.Ldloc(v3);
            il.Ret();
        }
    }
}