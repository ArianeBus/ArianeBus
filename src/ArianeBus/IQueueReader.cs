namespace ArianeBus;

internal interface IQueueReader
{
	string QueueOrTopicName { get; set; }
	Type MessageType { get; set; }
	Type ReaderType { get; set; }
}
