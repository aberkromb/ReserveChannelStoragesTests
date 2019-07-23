using System;
using ZeroFormatter;

namespace Generator
{
    [ZeroFormattable]
    public class MessageData
    {
        [Index(0)]
        public Guid Id { get; set; }
        [Index(1)]
        public string Server { get; set; }
        [Index(2)]
        public string Application { get; set; }
        [Index(3)]
        public string Exchange { get; set; }
        [Index(4)]
        public DateTimeOffset MessageDate { get; set; }
        [Index(5)]
        public string MessageType { get; set; }
        [Index(6)]
        public string MessageRoutingKey { get; set; }
        [Index(7)]
        public string Message { get; set; }
        [Index(8)]
        public string Exception { get; set; }
        [Index(9)]
        public int? Ttl { get; set; }
        [Index(10)]
        public bool Persistent { get; set; }
        [Index(11)]
        public string AdditionalHeaders { get; set; }
    }
}