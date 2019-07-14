using System.Runtime.CompilerServices;
using Utf8Json;

namespace ReserveChannelStoragesTests.Json
{
    public class Utf8JsonWrapper : IJsonService
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Serialize<T>(T obj) => JsonSerializer.ToJsonString(obj);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json);
    }
}