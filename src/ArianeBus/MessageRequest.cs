using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus;

internal class MessageRequest 
{
    public string QueueOrTopicName { get; set; } = null!;
    public object Message { get; set; } = default!;
    public string AssemblyQualifiedNameMessageType => Message.GetType().AssemblyQualifiedName!;
    internal QueueType QueueType { get; set; }
    public MessageOptions? MessageOptions { get; set; }
}
