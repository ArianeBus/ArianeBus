namespace ArianeBus;

public abstract class SendMessageStrategyBase
{
	protected int _messageAddedCount = 0;
	protected int _messageProcessedCount = 0;
	protected int _messageSentCount = 0;

	public abstract string StrategyName { get; }
    public abstract Task TrySendRequest(ServiceBusSender sender, MessageRequest messageRequest, CancellationToken cancellationToken);

	public virtual (int messageAddedCount, int messageProcessedCount, int messageSentCount) GetStats()
	{
		return (_messageAddedCount, _messageProcessedCount, _messageSentCount);
	}

	internal virtual ServiceBusMessage CreateServiceBusMessage(MessageRequest messageRequest)
	{
		var data = System.Text.Json.JsonSerializer.Serialize(messageRequest.Message);
		var bdata = Encoding.UTF8.GetBytes(data);
		var busMessage = new ServiceBusMessage(bdata);
		if (messageRequest.MessageOptions is not null)
		{
			busMessage.Subject = messageRequest.MessageOptions.Subject;
			if (messageRequest.MessageOptions.TimeToLive.HasValue)
			{
				busMessage.TimeToLive = messageRequest.MessageOptions.TimeToLive.Value;
			}
			if (messageRequest.MessageOptions.ScheduledEnqueueTimeUtc.HasValue)
			{
				busMessage.ScheduledEnqueueTime = messageRequest.MessageOptions.ScheduledEnqueueTimeUtc.Value;
			}
		}
		return busMessage;
	}
}
