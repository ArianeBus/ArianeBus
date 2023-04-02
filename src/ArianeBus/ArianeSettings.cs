using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus;

public class ArianeSettings
{
    public string BusConnectionString { get; set; } = null!;
    public int DefaultMessageTimeToLiveInDays { get; set; } = 1;
    public int AutoDeleteOnIdleInDays { get; set; } = 7;
    public int MaxDeliveryCount { get; set; } = 1;
    public int BatchSendingBufferSize { get; set; } = 20;
    public string SendStrategyName { get; set; } = $"{SendStrategy.Bufferized}";
    public string? PrefixName { get; set; }
    public int ReceiveMessageBufferSize { get; set; } = 10;
    public int ReceiveMessageTimeoutInSecond { get; set; } = 1;
    internal List<TopicReaderRegistration> TopicReaderList { get; set; } = new();
    internal List<QueueReaderRegistration> QueueReaderList { get; set; } = new();

}
