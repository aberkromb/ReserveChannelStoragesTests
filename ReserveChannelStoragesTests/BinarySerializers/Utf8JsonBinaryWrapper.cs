using System.Runtime.CompilerServices;
using ReserveChannelStoragesTests.BinarySerializers;
using ReserveChannelStoragesTests.JsonSerializers;
using Utf8Json;

namespace ReserveChannelStoragesTests.Json
{
    public class Utf8JsonBinaryWrapper : IBinarySerializer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] Serialize<T>(T obj) => JsonSerializer.Serialize(obj);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Deserialize<T>(byte[] json) => JsonSerializer.Deserialize<T>(json);
    }
}