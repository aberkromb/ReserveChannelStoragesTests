using System;
using Generator;

namespace ReserveChannelStoragesTests
{
    public class Filters
    {
        private static DateTime dt = DateTime.Now.AddDays(-15);

        public static bool IsFiltersPassed(MessageData data) => data.MessageDate < dt;
    }
}