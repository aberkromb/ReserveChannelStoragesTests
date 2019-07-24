using System;
using Generator;

namespace ReserveChannelStoragesTests
{
    public class Condition
    {
        private static DateTime dt = DateTime.Now.AddDays(-15);

        public static bool Predicate(MessageData data) => data.MessageDate < dt;
    }
}