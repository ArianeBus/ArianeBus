using ArianeBus.MediatR;

namespace MediatR;

public static class MediatRExtensions
{
	public static async Task GlobalPublish(this IMediator mediator, INotification notification, CancellationToken cancellationToken = default)
	{
		if (notification is null)
		{
			throw new ArgumentNullException(nameof(notification));
		}
		var busConfig = await mediator.Send(new GetBusRequest(), cancellationToken);
		var aqn = notification.GetType().AssemblyQualifiedName!.Split(',');
		var simplifiedAqn = $"{aqn[0]},{aqn[1]}";
		var message = new NotificationMessage
		{
			SerializedNotification = System.Text.Json.JsonSerializer.Serialize(notification, ArianeBus.JsonSerializer.Options),
			NotificationFullTypeName = simplifiedAqn
		};
		await busConfig.Bus.PublishTopic(busConfig.Configuration.TopicName, message, cancellationToken: cancellationToken);
	}
}
