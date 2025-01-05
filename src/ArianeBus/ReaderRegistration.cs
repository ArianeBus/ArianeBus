namespace ArianeBus;

internal class ReaderRegistration
{
	public string QueueOrTopicName { get; set; } = null!;
	public string SubscriptionName { get; set; } = null!;
	internal Type ReaderType { get; set; } = default!;
	internal QueueType QueueType { get; set; } = QueueType.Queue;
	public bool IsRegistered { get; set; } = false;
}
