using ArianeBus.MediatR;

namespace MediatR;

public static class MediatRExtensions
{
	public static async Task GlobalPublish<T>(this IMediator mediator, T notification, CancellationToken cancellationToken = default)
		where T : INotification
	{
		if (notification is null)
		{
			throw new ArgumentNullException(nameof(notification));
		}
		var busConfig = await mediator.Send(new GetBusRequest(), cancellationToken);
		var aqn = notification.GetType().AssemblyQualifiedName!.Split(',');
		var simplifiedAqn = $"{aqn[0]},{aqn[1]}";
		string notifString = System.Text.Json.JsonSerializer.Serialize(notification, ArianeBus.JsonSerializer.Options);
		var message = new NotificationMessage
		{
			SerializedNotification = notifString,
			NotificationFullTypeName = simplifiedAqn
		};
		await busConfig.Bus.PublishTopic(busConfig.Configuration.TopicName, message, cancellationToken: cancellationToken);
	}
}
