using System;

namespace HardTransferObject
{
    public class ProxyMapping
    {
        private readonly Func<object, object> baseToProxyConverter;
        private readonly Func<object, object> proxyToBaseConverter;

        public ProxyMapping(Type proxyType, Func<object, object> baseToProxyConverter, Func<object, object> proxyToBaseConverter)
        {
            this.baseToProxyConverter = baseToProxyConverter;
            this.proxyToBaseConverter = proxyToBaseConverter;
            ProxyType = proxyType;
        }

        public Type ProxyType { get; }

        public object Serialize(object @base)
        {
            return baseToProxyConverter(@base);
        }

        public object Deserialize(object proxy)
        {
            return proxyToBaseConverter(proxy);
        }
    }
}
