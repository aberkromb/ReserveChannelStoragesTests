using ZeroFormatter;

namespace ReserveChannelStoragesTests.BinarySerializers
{
    public class ZeroFormatterWrapper : IBinarySerializer
    {
        public byte[] Serialize<T>(T obj) => ZeroFormatterSerializer.Serialize(obj);

        public T Deserialize<T>(byte[] bytes) => ZeroFormatterSerializer.Deserialize<T>(bytes);
    }
}