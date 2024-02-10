using MediatR;

namespace ArianeBus.MediatR;
public class NotificationMessage
{
	public string SerializedNotification { get; set; } = default!;
	public string NotificationFullTypeName { get; set; } = null!;
}
