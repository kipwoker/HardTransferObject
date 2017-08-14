using System;
using System.Collections.Generic;
using System.Linq;

namespace HardTransferObject
{
    public static class ReflectionExtensions
    {
        public static bool IsIEnumerableInterface(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        public static bool IsIEnumerableInterfaceImplementation(this Type type)
        {
            return type.GetInterfaces().Any(x => x.IsIEnumerableInterface());
        }

        public static Type GetEnumerableType(this Type type)
        {
            if (type.IsIEnumerableInterface())
            {
                return type.GetGenericArguments()[0];
            }

            return type
                .GetInterfaces()
                .FirstOrDefault(x => x.IsIEnumerableInterface())?
                .GetGenericArguments()[0];
        }
    }
}