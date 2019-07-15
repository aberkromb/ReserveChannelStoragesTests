namespace ReserveChannelStoragesTests.BinarySerializers
{
    public interface IBinarySerializer
    {
        byte[] Serialize<T>(T obj);
        T Deserialize<T>(byte[] bytes);
    }
}