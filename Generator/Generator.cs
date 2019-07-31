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
                   .RuleFor(o => o.Id, f => f.IndexFaker)
                   .RuleFor(o => o.Server, f => f.Name.FirstName())
                   .RuleFor(o => o.Application, f => f.Name.LastName())
                   .RuleFor(o => o.Exchange, f => f.Name.JobTitle())
                   .RuleFor(o => o.MessageDate, f => f.Date.BetweenOffset(DateTime.Now.AddMonths(-1), DateTime.Now))
                   .RuleFor(o => o.MessageType, f => f.Lorem.Word())
                   .RuleFor(o => o.MessageRoutingKey, f => f.Lorem.Word())
                   .RuleFor(o => o.Message, f => f.Lorem.Sentence(300))
                   .RuleFor(o => o.Exception, f => f.Lorem.Word())
                   .RuleFor(o => o.Ttl, f => f.Random.Int())
                   .RuleFor(o => o.Persistent, f => f.Random.Bool())
                   .RuleFor(o => o.AdditionalHeaders, f => f.Lorem.Sentence(4));
        }
    }
}