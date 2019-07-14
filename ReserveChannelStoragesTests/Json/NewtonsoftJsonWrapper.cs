using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace ReserveChannelStoragesTests.Json
{
    public class NewtonsoftJsonWrapper : IJsonService
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json);
    }
}