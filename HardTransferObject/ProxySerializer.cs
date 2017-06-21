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
            var proxyMapping = proxyProvider.GetOrCreate(typeof(TBase));
            var proxy = serializer.Deserialize(serializedProxy, proxyMapping.ProxyType);
            var deserializedBase = proxyMapping.Deserialize<TBase>(proxy);
            return deserializedBase;
        }

        public byte[] Serialize<TBase>(TBase sample)
        {
            var proxyMapping = proxyProvider.GetOrCreate(typeof(TBase));
            var proxy = proxyMapping.Serialize(sample);
            var serializedProxy = serializer.Serialize(proxy, proxyMapping.ProxyType);
            return serializedProxy;
        }
    }
}