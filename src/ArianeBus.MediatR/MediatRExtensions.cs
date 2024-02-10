using MediatR;

namespace ArianeBus.MediatR;
public static class MediatRExtensions
{
	public static async Task GlobalPublish(this IMediator mediator, INotification notification, CancellationToken cancellationToken = default)
	{
		var busConfig = await mediator.Send(new GetBusRequest(), cancellationToken);
		var message = new NotificationMessage
		{
			SerializedNotification = System.Text.Json.JsonSerializer.Serialize(notification, ArianeBus.JsonSerializer.Options),
			NotificationFullTypeName = notification.GetType().FullName ?? string.Empty
		};
		await busConfig.Bus.PublishTopic(busConfig.Configuration.TopicName, message, cancellationToken: cancellationToken);
	}
}
