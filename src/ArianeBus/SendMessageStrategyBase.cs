using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus;

public abstract class SendMessageStrategyBase
{
	protected int _messageAddedCount = 0;
	protected int _messageProcessedCount = 0;
	protected int _messageSentCount = 0;

	public abstract string StrategyName { get; }
    internal abstract Task TrySendRequest(MessageRequest messageRequest, CancellationToken cancellationToken);

	public (int messageAddedCount, int messageProcessedCount, int messageSentCount) GetStats()
	{
		return (_messageAddedCount, _messageProcessedCount, _messageSentCount);
	}

	internal virtual ServiceBusMessage CreateServiceBusMessage(MessageRequest messageRequest)
	{
		var data = System.Text.Json.JsonSerializer.Serialize(messageRequest.Message);
		var bdata = Encoding.UTF8.GetBytes(data);
		var busMessage = new ServiceBusMessage(bdata);
		return busMessage;
	}
}
