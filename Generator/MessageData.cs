using System;

namespace Generator
{
    public class MessageData
    {
        public Guid Id { get; set; }
        public string Server { get; set; }
        public string Application { get; set; }
        public string Exchange { get; set; }
        public DateTimeOffset MessageDate { get; set; }
        public string MessageType { get; set; }
        public string MessageRoutingKey { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
        public int? Ttl { get; set; }
        public bool Persistent { get; set; }
        public string AdditionalHeaders { get; set; }
    }
}