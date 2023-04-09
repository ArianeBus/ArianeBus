using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus;

public class ArianeSettings
{
    public string BusConnectionString { get; set; } = null!;
    public int DefaultMessageTimeToLiveInDays { get; set; } = 1;
    public int AutoDeleteOnIdleInDays { get; set; } = 7;
    public int MaxDeliveryCount { get; set; } = 1;
    public int BatchSendingBufferSize { get; set; } = 20;
    public string SendStrategyName { get; set; } = $"{SendStrategy.Bufferized}";
    public string? PrefixName { get; set; }
    public int ReceiveMessageBufferSize { get; set; } = 10;
    public int ReceiveMessageTimeoutInSecond { get; set; } = 1;

    public bool CreateQueueIfNotExists { get; set; } = true;
    public bool CreateTopicIfNotExists { get; set; } = true;
    public bool CreateSubscriptionIfNotExists { get; set; } = true;

    public bool UseMockForUnitTests { get; set; } = false;

    internal List<TopicReaderRegistration> TopicReaderList { get; set; } = new();
    internal List<QueueReaderRegistration> QueueReaderList { get; set; } = new();
    internal Dictionary<string, QueueOrTopicBehaviorOptions> MessageSendOptionsList { get; set; } = new();

	public ArianeSettings RegisterTopicReader<TReader>(TopicName topicName, SubscriptionName subscriptionName)
		where TReader : IMessageReader
	{
		return RegisterTopicReader(topicName, subscriptionName, typeof(TReader));
	}

	public ArianeSettings RegisterTopicReader(TopicName topicName, SubscriptionName subscriptionName, Type topicReaderType)
	{
		var topicReader = new TopicReaderRegistration
		{
			TopicName = topicName.Value,
			SubscriptionName = subscriptionName.Value,
			ReaderType = topicReaderType
		};

		return RegisterTopicReader(topicReader);
	}

	internal ArianeSettings RegisterTopicReader(TopicReaderRegistration topicReaderRegistration)
	{
		if (TopicReaderList.Any(i => i.TopicName.Equals(topicReaderRegistration.TopicName, StringComparison.InvariantCultureIgnoreCase)
												&& i.SubscriptionName.Equals(topicReaderRegistration.SubscriptionName, StringComparison.InvariantCultureIgnoreCase)))
		{
			return this;
		}
		TopicReaderList.Add(topicReaderRegistration);
		return this;
	}

	public ArianeSettings RegisterQueueReader<TReader>(QueueName queueName)
		where TReader : IMessageReader
	{
		return RegisterQueueReader(queueName, typeof(TReader));
	}

	public ArianeSettings RegisterQueueReader(QueueName queueName, Type queueReaderType)
	{
		var queueReader = new QueueReaderRegistration
		{
			QueueName = queueName.Value,
			ReaderType = queueReaderType
		};
		return RegisterQueueReader(queueReader);
	}

	internal ArianeSettings RegisterQueueReader(QueueReaderRegistration queueReaderRegistration)
	{
		if (QueueReaderList.Any(i => i.QueueName.Equals(queueReaderRegistration.QueueName, StringComparison.InvariantCultureIgnoreCase)))
		{
			return this;
		}
		QueueReaderList.Add(queueReaderRegistration);
		return this;
	}
}
