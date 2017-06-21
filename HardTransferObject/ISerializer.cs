using System;

namespace HardTransferObject
{
    public interface ISerializer
    {
        object Deserialize(byte[] data, Type type);
        byte[] Serialize(object data, Type type);
    }
}