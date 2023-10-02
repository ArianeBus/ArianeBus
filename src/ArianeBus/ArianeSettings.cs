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
    public string SendStrategyName { get; set; } = $"{SendStrategy.OneByOne}";
    public string? PrefixName { get; set; }
    public int ReceiveMessageBufferSize { get; set; } = 10;
    public int ReceiveMessageTimeoutInSecond { get; set; } = 1;

    public bool CreateQueueIfNotExists { get; set; } = true;
    public bool CreateTopicIfNotExists { get; set; } = true;
    public bool CreateSubscriptionIfNotExists { get; set; } = true;

    public bool UseMockForUnitTests { get; set; } = false;
	public ServiceBusTransportType ServiceBusTransportType { get; set; } = ServiceBusTransportType.AmqpTcp;

    internal List<ReaderRegistration> ReaderList { get; set; } = new();
    internal Dictionary<string, QueueOrTopicBehaviorOptions> MessageSendOptionsList { get; set; } = new();

	public ArianeSettings RegisterTopicReader<TReader>(TopicName topicName, SubscriptionName subscriptionName)
		where TReader : IMessageReader
	{
		return RegisterTopicReader(topicName, subscriptionName, typeof(TReader));
	}

	public ArianeSettings RegisterTopicReader(TopicName topicName, SubscriptionName subscriptionName, Type topicReaderType)
	{
		var topicReader = new ReaderRegistration
		{
			QueueOrTopicName = topicName.Value,
			SubscriptionName = subscriptionName.Value,
			ReaderType = topicReaderType,
			QueueType = QueueType.Topic
		};

		return RegisterTopicReader(topicReader);
	}

	internal ArianeSettings RegisterTopicReader(ReaderRegistration topicReaderRegistration)
	{
		if (ReaderList.Any(i => i.QueueOrTopicName.Equals(topicReaderRegistration.QueueOrTopicName, StringComparison.InvariantCultureIgnoreCase)
												&& i.SubscriptionName.Equals(topicReaderRegistration.SubscriptionName, StringComparison.InvariantCultureIgnoreCase)))
		{
			return this;
		}
		ReaderList.Add(topicReaderRegistration);
		return this;
	}

	public ArianeSettings RegisterQueueReader<TReader>(QueueName queueName)
		where TReader : IMessageReader
	{
		return RegisterQueueReader(queueName, typeof(TReader));
	}

	public ArianeSettings RegisterQueueReader(QueueName queueName, Type queueReaderType)
	{
		var queueReader = new ReaderRegistration
		{
			QueueOrTopicName = queueName.Value,
			ReaderType = queueReaderType,
			QueueType = QueueType.Queue
		};
		return RegisterQueueReader(queueReader);
	}

	public void RegisterQueueOrTopicBehaviorOptions(string queueOrTopicName, Action<QueueOrTopicBehaviorOptions> action)
	{
		var messageSendingOptions = new QueueOrTopicBehaviorOptions();
		action(messageSendingOptions);
		RegisterQueueOrTopicBehaviorOptions(queueOrTopicName, messageSendingOptions);
	}

	public void RegisterQueueOrTopicBehaviorOptions(string queueOrTopicName, QueueOrTopicBehaviorOptions messageSendingOptions)
	{
		if (string.IsNullOrWhiteSpace(queueOrTopicName))
		{
			throw new ArgumentNullException(nameof(queueOrTopicName));
		}
		if (MessageSendOptionsList.Any(i => i.Key.Equals(queueOrTopicName, StringComparison.InvariantCultureIgnoreCase)))
		{
			return;
		}
		MessageSendOptionsList.Add(queueOrTopicName, messageSendingOptions);
	}


	internal ArianeSettings RegisterQueueReader(ReaderRegistration queueReaderRegistration)
	{
		if (ReaderList.Any(i => i.QueueOrTopicName.Equals(queueReaderRegistration.QueueOrTopicName, StringComparison.InvariantCultureIgnoreCase)))
		{
			return this;
		}
		ReaderList.Add(queueReaderRegistration);
		return this;
	}
}
