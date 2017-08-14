using System;
using System.Reflection;
using System.Reflection.Emit;

namespace HardTransferObject
{
    public static class ILGeneratorExtensions
    {
        public static void Brfalse(this ILGenerator il, Label label)
        {
            Emit(il, OpCodes.Brfalse, label);
        }

        public static void BrtrueS(this ILGenerator il, Label label)
        {
            Emit(il, OpCodes.Brtrue_S, label);
        }

        public static void Br(this ILGenerator il, Label label)
        {
            Emit(il, OpCodes.Br, label);
        }

        public static void BrS(this ILGenerator il, Label label)
        {
            Emit(il, OpCodes.Br_S, label);
        }

        public static void Call(this ILGenerator il, ConstructorInfo constructor)
        {
            Emit(il, OpCodes.Call, constructor);
        }

        public static void Call(this ILGenerator il, MethodInfo method)
        {
            Emit(il, OpCodes.Call, method);
        }

        public static void Callvirt(this ILGenerator il, MethodInfo method)
        {
            Emit(il, OpCodes.Callvirt, method);
        }

        public static void Castclass(this ILGenerator il, Type type)
        {
            Emit(il, OpCodes.Castclass, type);
        }

        public static LocalBuilder DeclreLocal(this ILGenerator il, Type localType, bool pinned = false)
        {
            return il.DeclareLocal(localType, pinned);
        }

        public static void Unbox_Any(this ILGenerator il, Type type)
        {
            Emit(il, OpCodes.Unbox_Any, type);
        }

        public static void Ldarg(this ILGenerator il, int argIndex)
        {
            switch (argIndex)
            {
                case 0: Emit(il, OpCodes.Ldarg_0); break;
                case 1: Emit(il, OpCodes.Ldarg_1); break;
                case 2: Emit(il, OpCodes.Ldarg_2); break;
                case 3: Emit(il, OpCodes.Ldarg_3); break;

                default:
                    if (argIndex < 256)
                    {
                        Emit(il, OpCodes.Ldarg_S, (byte)argIndex);
                    }
                    else
                    {
                        Emit(il, OpCodes.Ldarg, argIndex);
                    }
                    break;
            }
        }

        public static void Ldarga(this ILGenerator il, int argIndex)
        {
            if (argIndex < 256)
            {
                Emit(il, OpCodes.Ldarga_S, (byte)argIndex);
            }
            else
            {
                Emit(il, OpCodes.Ldarga, (short)argIndex);
            }
        }

        public static void Add(this ILGenerator il)
        {
            Emit(il, OpCodes.Add);
        }

        public static void Ldc_I4(this ILGenerator il, int constant)
        {
            switch (constant)
            {
                case 0: Emit(il, OpCodes.Ldc_I4_0); break;
                case 1: Emit(il, OpCodes.Ldc_I4_1); break;
                case 2: Emit(il, OpCodes.Ldc_I4_2); break;
                case 3: Emit(il, OpCodes.Ldc_I4_3); break;
                case 4: Emit(il, OpCodes.Ldc_I4_4); break;
                case 5: Emit(il, OpCodes.Ldc_I4_5); break;
                case 6: Emit(il, OpCodes.Ldc_I4_6); break;
                case 7: Emit(il, OpCodes.Ldc_I4_7); break;
                case 8: Emit(il, OpCodes.Ldc_I4_8); break;
                case -1: Emit(il, OpCodes.Ldc_I4_M1); break;
                default:
                    if (constant < 128 && constant >= -128)
                    {
                        Emit(il, OpCodes.Ldc_I4_S, (sbyte)constant);
                    }
                    else
                    {
                        Emit(il, OpCodes.Ldc_I4, constant);
                    }
                    break;
            }
        }

        public static void Ldfld(this ILGenerator il, FieldInfo field)
        {
            Emit(il, OpCodes.Ldfld, field);
        }

        public static void Ldftn(this ILGenerator il, MethodInfo method)
        {
            Emit(il, OpCodes.Ldftn, method);
        }

        public static void Ldelem_Ref(this ILGenerator il)
        {
            Emit(il, OpCodes.Ldelem_Ref);
        }

        public static void Ldloc(this ILGenerator il, LocalBuilder local)
        {
            switch (local.LocalIndex)
            {
                case 0: Emit(il, OpCodes.Ldloc_0); break;
                case 1: Emit(il, OpCodes.Ldloc_1); break;
                case 2: Emit(il, OpCodes.Ldloc_2); break;
                case 3: Emit(il, OpCodes.Ldloc_3); break;

                default:
                    if (local.LocalIndex < 256)
                    {
                        Emit(il, OpCodes.Ldloc_S, (byte)local.LocalIndex);
                    }
                    else
                    {
                        Emit(il, OpCodes.Ldloc, (short)local.LocalIndex);
                    }
                    break;
            }
        }

        public static void LdLen(this ILGenerator il)
        {
            Emit(il, OpCodes.Ldlen);
        }

        public static void Clt(this ILGenerator il)
        {
            Emit(il, OpCodes.Clt);
        }

        public static void ConvI4(this ILGenerator il)
        {
            Emit(il, OpCodes.Conv_I4);
        }

        public static void Ldloca(this ILGenerator il, LocalBuilder local)
        {
            if (local.LocalIndex < 256)
            {
                Emit(il, OpCodes.Ldloca_S, (byte)local.LocalIndex);
            }
            else
            {
                Emit(il, OpCodes.Ldloca, (short)local.LocalIndex);
            }
        }

        public static void Ldnull(this ILGenerator il)
        {
            Emit(il, OpCodes.Ldnull);
        }

        public static void Ldstr(this ILGenerator il, string constant)
        {
            Emit(il, OpCodes.Ldstr, constant);
        }

        public static void Ldsfld(this ILGenerator il, FieldInfo fieldInfo)
        {
            Emit(il, OpCodes.Ldsfld, fieldInfo);
        }

        public static void Ldtoken(this ILGenerator il, Type type)
        {
            Emit(il, OpCodes.Ldtoken, type);
        }

        public static void Ldind(this ILGenerator il, Type type)
        {
            if (!type.IsValueType) //class
            {
                Ldind_Ref(il);
                return;
            }

            if (IsStruct(type)) //struct
            {
                Ldobj(il, type);
                return;
            }

            switch (Type.GetTypeCode(type)) //primitive
            {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    Emit(il, OpCodes.Ldind_I1);
                    break;
                case TypeCode.Int16:
                case TypeCode.Char:
                case TypeCode.UInt16:
                    Emit(il, OpCodes.Ldind_I2);
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    Emit(il, OpCodes.Ldind_I4);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    Emit(il, OpCodes.Ldind_I8);
                    break;
                case TypeCode.Single:
                    Emit(il, OpCodes.Ldind_R4);
                    break;
                case TypeCode.Double:
                    Emit(il, OpCodes.Ldind_R8);
                    break;
                default:
                    throw new NotSupportedException("Type '" + type.FullName + "' is not supported");
            }
        }

        public static void Ldobj(this ILGenerator il, Type type)
        {
            Emit(il, OpCodes.Ldobj, type);
        }

        public static void Ldind_Ref(this ILGenerator il)
        {
            Emit(il, OpCodes.Ldind_Ref);
        }

        public static void Newarr(this ILGenerator il, Type elemType)
        {
            Emit(il, OpCodes.Newarr, elemType);
        }

        public static void Newobj(this ILGenerator il, ConstructorInfo constructor)
        {
            Emit(il, OpCodes.Newobj, constructor);
        }

        public static void Pop(this ILGenerator il)
        {
            Emit(il, OpCodes.Pop);
        }

        public static void Ret(this ILGenerator il)
        {
            Emit(il, OpCodes.Ret);
        }

        public static void Stfld(this ILGenerator il, FieldInfo field)
        {
            Emit(il, OpCodes.Stfld, field);
        }

        public static void Stloc(this ILGenerator il, LocalBuilder local)
        {
            switch (local.LocalIndex)
            {
                case 0: Emit(il, OpCodes.Stloc_0); break;
                case 1: Emit(il, OpCodes.Stloc_1); break;
                case 2: Emit(il, OpCodes.Stloc_2); break;
                case 3: Emit(il, OpCodes.Stloc_3); break;

                default:
                    if (local.LocalIndex < 256)
                    {
                        Emit(il, OpCodes.Stloc_S, (byte)local.LocalIndex);
                    }
                    else
                    {
                        Emit(il, OpCodes.Stloc, (short)local.LocalIndex);
                    }
                    break;
            }
        }

        public static void Stind(this ILGenerator il, Type type)
        {
            if (!type.IsValueType) //class
            {
                Stind_Ref(il);
                return;
            }

            if (IsStruct(type)) //struct
            {
                Stobj(il, type);
                return;
            }

            switch (Type.GetTypeCode(type)) //primitive
            {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    Emit(il, OpCodes.Stind_I1);
                    break;
                case TypeCode.Int16:
                case TypeCode.Char:
                case TypeCode.UInt16:
                    Emit(il, OpCodes.Stind_I2);
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    Emit(il, OpCodes.Stind_I4);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    Emit(il, OpCodes.Stind_I8);
                    break;
                case TypeCode.Single:
                    Emit(il, OpCodes.Stind_R4);
                    break;
                case TypeCode.Double:
                    Emit(il, OpCodes.Stind_R8);
                    break;
                default:
                    throw new NotSupportedException("Type '" + type.FullName + "' is not supported");
            }
        }

        public static void Stobj(this ILGenerator il, Type type)
        {
            Emit(il, OpCodes.Stobj, type);
        }

        public static void Stind_Ref(this ILGenerator il)
        {
            Emit(il, OpCodes.Stind_Ref);
        }

        public static void Dup(this ILGenerator il)
        {
            Emit(il, OpCodes.Dup);
        }

        public static void Stelem_Ref(this ILGenerator il)
        {
            Emit(il, OpCodes.Stelem_Ref);
        }

        public static void Box(this ILGenerator il, Type boxingType)
        {
            Emit(il, OpCodes.Box, boxingType);
        }

        public static void LdlocAsRefType(this ILGenerator il, LocalBuilder localBuilder)
        {
            il.Ldloc(localBuilder);
            if (localBuilder.LocalType.IsValueType)
            {
                il.Box(localBuilder.LocalType);
            }
        }

        #region private stuff
        private static void Emit(ILGenerator il, OpCode opCode)
        {
            il.Emit(opCode);
        }

        private static void Emit(ILGenerator il, OpCode opCode, byte i)
        {
            il.Emit(opCode, i);
        }

        private static void Emit(ILGenerator il, OpCode opCode, int i)
        {
            il.Emit(opCode, i);
        }

        private static void Emit(ILGenerator il, OpCode opCode, sbyte i)
        {
            il.Emit(opCode, i);
        }

        private static void Emit(ILGenerator il, OpCode opCode, short i)
        {
            il.Emit(opCode, i);
        }

        private static void Emit(ILGenerator il, OpCode opCode, Type type)
        {
            il.Emit(opCode, type);
        }

        private static void Emit(ILGenerator il, OpCode opCode, LocalBuilder localVar)
        {
            il.Emit(opCode, localVar);
        }

        private static void Emit(ILGenerator il, OpCode opCode, ConstructorInfo constructor)
        {
            il.Emit(opCode, constructor);
        }

        private static void Emit(ILGenerator il, OpCode opCode, MethodInfo method)
        {
            il.Emit(opCode, method);
        }

        private static void Emit(ILGenerator il, OpCode opCode, FieldInfo field)
        {
            il.Emit(opCode, field);
        }

        private static void Emit(ILGenerator il, OpCode opCode, string constant)
        {
            il.Emit(opCode, constant);
        }

        private static void Emit(ILGenerator il, OpCode opCode, Label label)
        {
            il.Emit(opCode, label);
        }

        private static bool IsStruct(Type type)
        {
            return type.IsValueType &&
                   !type.IsPrimitive &&
                   !type.IsEnum &&
                   type != typeof(IntPtr) &&
                   type != typeof(UIntPtr);
        }
        #endregion
    }
}