namespace ArianeBus;

internal class SendAction
{
	public ServiceBusSender Sender { get; set; } = default!;
	public Action Action { get; set; } = default!;
}
