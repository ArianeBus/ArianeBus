namespace ArianeBus;

internal interface ITopicReader
{
	string QueueOrTopicName { get; set; }
	string SubscriptionName { get; set; }
	Type MessageType { get; set; }
	Type ReaderType { get; set; }
}
