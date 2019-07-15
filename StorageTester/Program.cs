using System.Text;
using System.Threading;
using Aerospike.Client;
using Newtonsoft.Json;
using ReserveChannelStoragesTests;
using ReserveChannelStoragesTests.AerospikeDataAccessImplementation;
using ReserveChannelStoragesTests.Json;

namespace StorageTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var users = Generator.UserGenerator.CreateRandomUsers(1);

            var asyncClient = new AsyncClient("localhost", 3000);
            var dataAccess = new AerospikeDataProvider(asyncClient, new NewtonsoftJsonWrapper());


            foreach (var user in users)
            {
                dataAccess.Add(new AerospikeDataObject
                               {
                                   Key = 1,
                                   Namespace = "reserve_channel",
                                   SetName = "messages",
                                   Data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(user))
                               },
                               CancellationToken.None);
            }
        }
    }
}