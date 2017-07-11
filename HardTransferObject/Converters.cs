using System.Collections.Generic;

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
}