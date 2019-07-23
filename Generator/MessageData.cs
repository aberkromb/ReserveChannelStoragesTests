using System;
using ZeroFormatter;

namespace Generator
{
    [ZeroFormattable]
    public class MessageData
    {
        [Index(0)]
        public virtual  Guid Id { get; set; }
        [Index(1)]
        public virtual  string Server { get; set; }
        [Index(2)]
        public virtual  string Application { get; set; }
        [Index(3)]
        public virtual  string Exchange { get; set; }
        [Index(4)]
        public virtual  DateTimeOffset MessageDate { get; set; }
        [Index(5)]
        public virtual  string MessageType { get; set; }
        [Index(6)]
        public virtual  string MessageRoutingKey { get; set; }
        [Index(7)]
        public virtual  string Message { get; set; }
        [Index(8)]
        public virtual  string Exception { get; set; }
        [Index(9)]
        public virtual  int? Ttl { get; set; }
        [Index(10)]
        public virtual  bool Persistent { get; set; }
        [Index(11)]
        public virtual  string AdditionalHeaders { get; set; }
    }
}