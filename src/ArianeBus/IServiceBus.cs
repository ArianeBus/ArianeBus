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

	Task CreateTopic(TopicName topicName, SubscriptionName subscription, CancellationToken cancellationToken = default);
}
