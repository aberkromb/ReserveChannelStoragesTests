using System;
using System.Collections.Generic;
using Bogus;

namespace Generator
{
    public class UserGenerator
    {
        public static List<User> CreateRandomUsers(int count)
        {
            var id = 0;
            return new Faker<User>()
                .RuleFor(user => user.Id, f => id++)
                .RuleFor(user => user.FirstName, f => f.Name.FirstName())
                .RuleFor(user => user.LastName, f => f.Name.LastName())
                .RuleFor(user => user.Address, f => new Address
                {
                    Country = f.Address.Country(),
                    City = f.Address.City(),
                    Street = f.Address.StreetName(),
                    HouseNumber = f.Random.Int()
                })
                .RuleFor(user => user.Job, f => f.Name.JobTitle())
                .RuleFor(user => user.Birthday, f => f.Date.Soon())
                .RuleFor(user => user.Interests, f => f.Random.WordsArray(5, 10))
                .RuleFor(user => user.About, f => new string(f.Random.Chars(max: (char)100,count: 100)))
                .Generate(count);
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