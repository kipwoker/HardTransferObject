using System;
using System.Collections.Generic;

namespace HardTransferObject
{
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
}