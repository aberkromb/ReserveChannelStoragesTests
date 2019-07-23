using System;
using ReserveChannelStoragesTests.Json;

namespace ReserveChannelStoragesTests.JsonSerializers
{
    public class JsonServiceFactory
    {
        public static IJsonService GetSerializer(string name)
        {
            switch (name.ToLower())
            {
                case "utf8":
                    return new Utf8JsonWrapper();
                case "newtonsoft":
                    return new NewtonsoftJsonWrapper();
                default:
                    throw new NotImplementedException();
            }
        }
    }
}