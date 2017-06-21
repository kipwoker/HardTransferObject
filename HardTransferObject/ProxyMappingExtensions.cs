namespace HardTransferObject
{
    public static class ProxyMappingExtensions
    {
        public static object Serialize<TBase>(this ProxyMapping proxyMapping, TBase @base)
        {
            return proxyMapping.Serialize(@base);
        }

        public static TBase Deserialize<TBase>(this ProxyMapping proxyMapping, object proxy)
        {
            return (TBase)proxyMapping.Deserialize(proxy);
        }
    }
}