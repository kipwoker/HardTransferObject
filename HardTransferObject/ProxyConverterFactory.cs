using System;
using System.Reflection.Emit;

namespace HardTransferObject
{
    public class ProxyConverterFactory : IProxyConverterFactory
    {
        private readonly ModuleBuilder moduleBuilder;
        private readonly IConverter<object, object> idealConverter = new IdealConverter();

        public ProxyConverterFactory(ModuleBuilder moduleBuilder)
        {
            this.moduleBuilder = moduleBuilder;
        }

        //todo: тут надо поэмитить, угадай где

        public IConverter<object, object> CreateConverterToProxy(Type baseType, Type proxyType)
        {
            if (baseType == proxyType)
            {
                return idealConverter;
            }

            return idealConverter;
        }

        public IConverter<object, object> CreateConverterToBase(Type baseType, Type proxyType)
        {
            if (baseType == proxyType)
            {
                return idealConverter;
            }

            return idealConverter;
        }
    }
}