using System;

namespace ReserveChannelStoragesTests
{
    public class ScriptConfig
    {
        public int BatchSize { get; set; } = 1000;
        public TimeSpan TimeToWrite { get; set; } = TimeSpan.FromMinutes(60);
        public int ParallelsCount { get; set; } = 10;
        public TimeSpan TimeToRead { get; set; } = TimeSpan.FromMinutes(60);
    }
}