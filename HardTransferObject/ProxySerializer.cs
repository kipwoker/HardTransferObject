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

            var proxy = serializer.Deserialize(serializedProxy, mappingChain.Last().ProxyType);
            return (TBase)mappingChain.Reverse().Aggregate(proxy, (current, t) => t.Serialize(current));
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