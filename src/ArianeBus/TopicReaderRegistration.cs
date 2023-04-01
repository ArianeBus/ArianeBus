using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus;

internal class TopicReaderRegistration
{
    public string TopicName { get; set; } = null!;
    public string SubscriptionName { get; set; } = null!;
    internal Type ReaderType { get; set; } = default!;
}
