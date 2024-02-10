namespace ArianeBus;

public interface IServiceBus
{
	/// <summary>
	/// Publish a message to a topic with a specific message options and cancellation token
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	/// <param name="topicName"></param>
	/// <param name="message"></param>
	/// <param name="options"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task PublishTopic<TMessage>(string topicName, TMessage message, MessageOptions? options = null, CancellationToken cancellationToken = default)
		where TMessage : class;

	/// <summary>
	/// Enqueue a message to a queue with a specific message options and cancellation token
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	/// <param name="queueName"></param>
	/// <param name="message"></param>
	/// <param name="options"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task EnqueueMessage<TMessage>(string queueName, TMessage message, MessageOptions? options = null, CancellationToken cancellationToken = default)
		where TMessage : class;

	/// <summary>
	/// For compatibility with previous version
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	/// <param name="topicOrQueueName"></param>
	/// <param name="message"></param>
	/// <param name="options"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	[Obsolete("Use PublishTopic or EnqueueMessage instead (this method does not create queue or topic)")]
	Task SendAsync<TMessage>(string topicOrQueueName, TMessage message, MessageOptions? options = null, CancellationToken cancellationToken = default)
		where TMessage : class;

	/// <summary>
	/// Receive a message from a queue with a specific message count, timeout and cancellation token
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	/// <param name="queueName"></param>
	/// <param name="messageCount"></param>
	/// <param name="timeoutInMillisecond"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<IEnumerable<TMessage>> ReceiveAsync<TMessage>(QueueName queueName, int messageCount, int timeoutInMillisecond, CancellationToken cancellationToken = default);

	/// <summary>
	/// Receive a message from a topic with a specific message count, timeout and cancellation token
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	/// <param name="topicName"></param>
	/// <param name="subscription"></param>
	/// <param name="messageCount"></param>
	/// <param name="timeoutInMillisecond"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<IEnumerable<TMessage>> ReceiveAsync<TMessage>(TopicName topicName, SubscriptionName subscription, int messageCount, int timeoutInMillisecond, CancellationToken cancellationToken = default);

	/// <summary>
	/// Create a queue with a specific cancellation token
	/// </summary>
	/// <param name="queueName"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task CreateQueue(QueueName queueName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Create a topic with a specific cancellation token
	/// </summary>
	/// <param name="topicName"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task CreateTopic(TopicName topicName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Create a subscription with a specific cancellation token
	/// </summary>
	/// <param name="topicName"></param>
	/// <param name="subscription"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task CreateTopicAndSubscription(TopicName topicName, SubscriptionName subscription, CancellationToken cancellationToken = default);

	/// <summary>
	/// Clear a queue with a specific cancellation token
	/// </summary>
	/// <param name="queueName"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task ClearQueue(QueueName queueName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Clear a topic with a specific cancellation token
	/// </summary>
	/// <param name="topicName"></param>
	/// <param name="subscriptionName"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task ClearTopic(TopicName topicName, SubscriptionName subscriptionName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Delete a queue with a specific cancellation token
	/// </summary>
	/// <param name="queueName"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task DeleteQueue(QueueName queueName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Delete a topic with a specific cancellation token
	/// </summary>
	/// <param name="topicName"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task DeleteTopic(TopicName topicName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Delete a subscription with a specific cancellation token
	/// </summary>
	/// <param name="topicName"></param>
	/// <param name="subscriptionName"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task DeleteSubscription(TopicName topicName, SubscriptionName subscriptionName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Is a queue exists with a specific cancellation token
	/// </summary>
	/// <param name="queueName"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<bool> IsQueueExists(QueueName queueName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Is a topic exists with a specific cancellation token
	/// </summary>
	/// <param name="topicName"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<bool> IsTopicExists(TopicName topicName, CancellationToken cancellationToken = default);

	/// <summary>
	/// IS a subscription exists with a specific cancellation token
	/// </summary>
	/// <param name="topicName"></param>
	/// <param name="subscriptionName"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<bool> IsSubscriptionExists(TopicName topicName, SubscriptionName subscriptionName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Get a list of registered queue names
	/// </summary>
	/// <returns></returns>
	IEnumerable<QueueName> GetRegisteredQueueNameList();

	/// <summary>
	/// Get a list of registered topic names
	/// </summary>
	/// <returns></returns>
	IDictionary<TopicName, SubscriptionName> GetRegisteredTopicAndSubscriptionNameList();

}
