using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus;

public interface IServiceBus
{
	Task PublishTopic<TMessage>(string topicName, TMessage message, MessageOptions? options = null, CancellationToken cancellationToken = default)
		where TMessage : class, new();

	Task EnqueueMessage<TMessage>(string queueName, TMessage message, MessageOptions? options = null, CancellationToken cancellationToken = default)
		where TMessage : class, new();

	Task<IEnumerable<TMessage>> ReceiveAsync<TMessage>(QueueName queueName, int count, int timeoutInMillisecond, CancellationToken cancellationToken = default);

	Task<IEnumerable<TMessage>> ReceiveAsync<TMessage>(TopicName topicName, SubscriptionName subscription, int count, int timeoutInMillisecond, CancellationToken cancellationToken = default);

	Task CreateQueue(QueueName queueName, CancellationToken cancellationToken = default);

	Task CreateTopic(TopicName topicName, CancellationToken cancellationToken = default);

	Task CreateTopicAndSubscription(TopicName topicName, SubscriptionName subscription, CancellationToken cancellationToken = default);

	Task ClearQueue(QueueName queueName, CancellationToken cancellationToken = default);

	Task ClearTopic(TopicName topicName, SubscriptionName subscriptionName, CancellationToken cancellationToken = default);

	Task DeleteQueue(QueueName queueName, CancellationToken cancellationToken = default);

	Task DeleteTopic(TopicName topicName, CancellationToken cancellationToken = default);

	Task DeleteSubscription(TopicName topicName, SubscriptionName subscriptionName, CancellationToken cancellationToken = default);

	Task<bool> IsQueueExists(QueueName queueName, CancellationToken cancellationToken = default);

	Task<bool> IsTopicExists(TopicName topicName, CancellationToken cancellationToken = default);

	Task<bool> IsSubscriptionExists(TopicName topicName, SubscriptionName subscriptionName, CancellationToken cancellationToken = default);

}
