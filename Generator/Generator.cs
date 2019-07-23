using System;
using System.Collections.Generic;
using Bogus;

namespace Generator
{
    public class Generator
    {
        public static List<MessageData> CreateRandomData(int count) => PrepareFaker().Generate(count);
        public static IEnumerable<MessageData> CreateRandomDataLazy(int count) => PrepareFaker().GenerateLazy(count);


        private static Faker<MessageData> PrepareFaker()
        {
            return new Faker<MessageData>()
                   .RuleFor(o => o.Id, f => Guid.NewGuid())
                   .RuleFor(o => o.Server, f => f.Name.FirstName())
                   .RuleFor(o => o.Application, f => f.Name.LastName())
                   .RuleFor(o => o.Exchange, f => f.Name.JobTitle())
                   .RuleFor(o => o.MessageDate, f => f.Date.BetweenOffset(DateTime.Now.AddMonths(-1), DateTime.Now))
                   .RuleFor(o => o.MessageType, f => f.Lorem.Word())
                   .RuleFor(o => o.MessageRoutingKey, f => f.Lorem.Word())
                   .RuleFor(o => o.Message, f => f.Lorem.Sentence(50))
                   .RuleFor(o => o.Exception, f => f.Lorem.Word())
                   .RuleFor(o => o.Ttl, f => f.Random.Int())
                   .RuleFor(o => o.Persistent, f => f.Random.Bool())
                   .RuleFor(o => o.AdditionalHeaders, f => f.Lorem.Sentence(4));
        }


        public class User
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public Address Address { get; set; }
            public string Job { get; set; }
            public DateTime Birthday { get; set; }
            public string[] Interests { get; set; }
            public string About { get; set; }
        }

        public class Address
        {
            public string Country { get; set; }
            public string City { get; set; }
            public string Street { get; set; }
            public int HouseNumber { get; set; }
        }
    }
}