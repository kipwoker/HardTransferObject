using System.Collections.Generic;
using System.Linq;

namespace HardTransferObject
{
    public static class CollectionConverter
    {
        public static T2[] Convert<T1, T2>(IEnumerable<T1> @in)
        {
            var array = @in.ToArray();
            var converted = new T2[array.Length];
            for (var i = 0; i < array.Length; ++i)
            {
                converted[i] = (T2)ConverterStorage
                    .Instance
                    .GetImplementation(typeof(T1), typeof(T2))
                    .Convert(array[i]);
            }

            return converted;
        }
    }
}