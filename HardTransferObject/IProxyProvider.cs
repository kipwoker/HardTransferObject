using System;

namespace HardTransferObject
{
    public interface IProxyProvider
    {
        ProxyMapping GetOrCreate(Type baseType);
    }
}