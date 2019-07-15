using System;
using ReserveChannelStoragesTests.JsonSerializers;

namespace ReserveChannelStoragesTests.Json
{
    public class JsonServiceFactory
    {
        public IJsonService GetSerializer(string name)
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