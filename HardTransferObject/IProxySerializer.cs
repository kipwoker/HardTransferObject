namespace HardTransferObject
{
    public interface IProxySerializer
    {
        TBase Deserialize<TBase>(byte[] serializedProxy);
        byte[] Serialize<TBase>(TBase sample);
    }
}