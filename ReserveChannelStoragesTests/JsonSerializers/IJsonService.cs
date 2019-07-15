namespace ReserveChannelStoragesTests.JsonSerializers
{
    public interface IJsonService
    {
        string Serialize<T>(T obj);
        T Deserialize<T>(string json);
    }
}