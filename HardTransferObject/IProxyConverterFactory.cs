using System;

namespace HardTransferObject
{
    public interface IProxyConverterFactory
    {
        IConverter<object, object> CreateConverterToProxy(Type baseType, Type proxyType);
        IConverter<object, object> CreateConverterToBase(Type baseType, Type proxyType);
    }
}