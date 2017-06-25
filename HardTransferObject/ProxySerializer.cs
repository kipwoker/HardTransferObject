using System.Linq;

namespace HardTransferObject
{
    public class ProxySerializer : IProxySerializer
    {
        private readonly IProxyProvider proxyProvider;
        private readonly ISerializer serializer;

        public ProxySerializer(
            IProxyProvider proxyProvider,
            ISerializer serializer)
        {
            this.proxyProvider = proxyProvider;
            this.serializer = serializer;
        }

        public TBase Deserialize<TBase>(byte[] serializedProxy)
        {
            var baseType = typeof(TBase);
            proxyProvider.Add(baseType);

            var mappingChain = proxyProvider.GetMappingChain(baseType);
            var proxyType = mappingChain.Last().ProxyType;

            var proxy = serializer.Deserialize(serializedProxy, proxyType);
            return (TBase)mappingChain.Reverse().Aggregate(proxy, (current, t) => t.Deserialize(current));
        }

        public byte[] Serialize<TBase>(TBase @base)
        {
            var baseType = typeof(TBase);
            proxyProvider.Add(baseType);

            var mappingChain = proxyProvider.GetMappingChain(baseType);

            var obj = mappingChain.Aggregate<ProxyMapping, object>(@base, (current, t) => t.Serialize(current));
            return serializer.Serialize(obj, obj.GetType());
        }
    }
}