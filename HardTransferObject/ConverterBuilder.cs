using System;
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

            var interfaceToObjectConverterType = CreateToObjectConverter(converterBuilder, methodBuilder, inType, outType);
            ConverterStorage.Instance.Add(inType, outType, interfaceToObjectConverterType);
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
}