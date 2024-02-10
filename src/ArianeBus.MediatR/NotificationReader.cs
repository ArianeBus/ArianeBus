using MediatR;

using Microsoft.Extensions.Logging;

namespace ArianeBus.MediatR;
internal class NotificationReader(
	IMediator mediator,
	ILogger<NotificationReader> logger
	)
	: ArianeBus.MessageReaderBase<NotificationMessage>
{
	public override async Task ProcessMessageAsync(NotificationMessage message, CancellationToken cancellationToken)
	{
		if (message is null)
		{
			logger.LogWarning("Received null message");
			return;
		}
		var type = Type.GetType(message.NotificationFullTypeName, true, true);
		if (type is null)
		{
			logger.LogWarning("Could not find type {NotificationFullTypeName}", message.NotificationFullTypeName);
			return;
		}
		var notification = System.Text.Json.JsonSerializer.Deserialize(message.SerializedNotification, type, ArianeBus.JsonSerializer.Options);
		if (notification is null)
		{
			logger.LogWarning("Could not deserialize notification of type {NotificationFullTypeName}", message.NotificationFullTypeName);
			return;
		}
		await mediator.Publish(notification, cancellationToken);
	}
}