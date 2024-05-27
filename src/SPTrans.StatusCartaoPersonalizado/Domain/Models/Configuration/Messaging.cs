using System;
using System.Collections.Generic;
using System.Text;

namespace SPTrans.StatusCartaoPersonalizado.Domain.Models.Configuration
{
    public class Messaging
    {
        public string Host { get; set; }
        public short Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public bool Durable { get; set; }
        public long TTL { get; set; }
        public short Retries { get; set; }
        public Consuming Consuming { get; set; }
        public Publishing Publishing { get; set; }
        public Publishing Error { get; set; }
    }

    public class Consuming
    {
        public string VirtualHost { get; set; }
        public string Queue { get; set; }
        public string Exchange { get; set; }
        public string Bindingkey { get; set; }
        public Deadletter Deadletter { get; set; }
    }

    public class Publishing
    {
        public string VirtualHost { get; set; }
        public string Exchange { get; set; }
        public string Routingkey { get; set; }
        public string Queue { get; set; }
        public Deadletter Deadletter { get; set; }
    }

    public class Deadletter
    {
        public string Queue { get; set; }
        public string Exchange { get; set; }
        public string Routingkey { get; set; }
    }
}
