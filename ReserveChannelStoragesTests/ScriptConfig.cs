using System;

namespace ReserveChannelStoragesTests
{
    public class ScriptConfig
    {
        public int GetBatchSize { get; set; } = 1000;
        public TimeSpan TimeToWrite { get; set; } = TimeSpan.FromMinutes(60);
        public int WriteParallelsCount { get; set; } = 100;
        public TimeSpan TimeToRead { get; set; } = TimeSpan.FromMinutes(600);
        public int ReadParallelsCount { get; set; } = 100;
    }
}