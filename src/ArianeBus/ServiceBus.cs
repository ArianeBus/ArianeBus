using System.Runtime.CompilerServices;
using System.Threading;

namespace ArianeBus;

internal class ServiceBus : IServiceBus
{
	private readonly IEnumerable<SendMessageStrategyBase> _senderStrategyList;
	private readonly ILogger _logger;
	private readonly ArianeSettings _settings;

	public ServiceBus(IEnumerable<SendMessageStrategyBase> senderStrategyList,
		ILogger<ServiceBus> logger,
		ArianeSettings settings)
    {
		_senderStrategyList = senderStrategyList;
		_logger = logger;
		_settings = settings;
	}

	public async Task PublishTopic<T>(string topicName, T message, MessageOptions? options = null, CancellationToken cancellationToken = default)
		where T : class, new()
	{
		if (message == null)
		{
			_logger.LogWarning("request is null");
			return;
		}

		if (string.IsNullOrWhiteSpace(topicName))
		{
			_logger.LogWarning("topicName name is null");
			return;
		}

		var messageRequest = new MessageRequest
		{
			Message = message,
			QueueOrTopicName = $"{_settings.PrefixName}{topicName}",
			QueueType = QueueType.Topic,
			MessageOptions = options
		};

		await SendInternal(messageRequest, cancellationToken);
	}

	public async Task EnqueueMessage<T>(string queueName, T message, MessageOptions? options = null, CancellationToken cancellationToken = default)
		where T : class, new()
	{
		if (message == null)
		{
			_logger.LogWarning("request is null");
			return;
		}

		if (string.IsNullOrWhiteSpace(queueName))
		{
			_logger.LogWarning("queue name is null");
			return;
		}

		var messageRequest = new MessageRequest
		{
			Message = message,
			QueueOrTopicName = $"{_settings.PrefixName}{queueName}",
			QueueType = QueueType.Queue,
			MessageOptions = options
		};

		await SendInternal(messageRequest, cancellationToken);
	}

	internal async Task SendInternal(MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		var strategyName = _settings.SendStrategyName;

		if (_settings.MessageSendOptionsList.Any(i => i.Key.Equals(messageRequest.QueueOrTopicName, StringComparison.InvariantCultureIgnoreCase)))
		{
			var queueOrTopicOptions = _settings.MessageSendOptionsList[messageRequest.QueueOrTopicName];
			strategyName = queueOrTopicOptions.SendStrategyName;
		}

		var sendStrategy = _senderStrategyList.SingleOrDefault(i => i.StrategyName.Equals(strategyName, StringComparison.InvariantCultureIgnoreCase));
		if (sendStrategy is null)
		{
			throw new ArgumentOutOfRangeException($"try to send message with unknown strategy {strategyName}");
		}

		await sendStrategy!.TrySendRequest(messageRequest, cancellationToken);
		_logger.LogTrace("send {Message} in queue {QueueOrTopicName}", messageRequest.Message, messageRequest.QueueOrTopicName);
	}

	public async Task<IEnumerable<TMessage>> ReceiveAsync<TMessage>(QueueName queueName, int messageCount, int timeoutInMillisecond, CancellationToken cancellationToken = default)
	{
		var client = _settings.CreateServiceBusClient();
		var name = $"{_settings.PrefixName}{queueName.Value}";
		var receiver = client.CreateReceiver(name, new ServiceBusReceiverOptions
		{
			PrefetchCount = 0,
			ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
		});

		return await ReceiveInternalAsync<TMessage>(receiver, messageCount, timeoutInMillisecond, cancellationToken);
	}

	public async Task<IEnumerable<TMessage>> ReceiveAsync<TMessage>(TopicName topicName, SubscriptionName subscription, int messageCount, int timeoutInMillisecond, CancellationToken cancellationToken = default)
	{
		var client = _settings.CreateServiceBusClient();
		var name = $"{_settings.PrefixName}{topicName.Value}";
		var receiver = client.CreateReceiver(name, subscription.Value, new ServiceBusReceiverOptions
		{
			PrefetchCount = 0,
			ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
		});

		return await ReceiveInternalAsync<TMessage>(receiver, messageCount, timeoutInMillisecond, cancellationToken);
	}

	private async Task<IEnumerable<TMessage>> ReceiveInternalAsync<TMessage>(ServiceBusReceiver receiver, int count, int timeoutInMillisecond, CancellationToken cancellationToken)
	{
		var result = new List<TMessage>();
		var loop = 0;

		IReadOnlyList<ServiceBusReceivedMessage>? receiveMessageList;
		while (true)
		{
			receiveMessageList = await receiver.ReceiveMessagesAsync(count, TimeSpan.FromMicroseconds(timeoutInMillisecond), cancellationToken);
			if ((receiveMessageList == null
				|| !receiveMessageList.Any()))
			{
				if (loop < 3)
				{
					// Workaround for startup receiver once with null list (I don't know why, maybe a bug)
					await Task.Delay(1 * 1000, cancellationToken);
					loop++;
					continue;
				}
				return result;
			}
			break;
		}

		foreach (var receiveMessage in receiveMessageList)
		{
			if (receiveMessage.Body == null)
			{
				_logger.LogWarning("receive message with body null on queue {queueName}", receiver.EntityPath);
				continue;
			}

			try
			{
				var message = System.Text.Json.JsonSerializer.Deserialize<TMessage>(receiveMessage.Body.ToString())!;
				result.Add(message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ErrorMessage : {message}", ex.Message);
			}
		}

		return result;
	}

	public async Task CreateQueue(QueueName queueName, CancellationToken cancellationToken = default)
	{
		var name = $"{_settings.PrefixName}{queueName.Value}";
		await _settings.CreateQueueIfNotExists(name, _logger, cancellationToken);
	}

	public async Task CreateTopic(TopicName topicName, CancellationToken cancellationToken = default)
	{
		var name = $"{_settings.PrefixName}{topicName.Value}";
		await _settings.CreateTopicIfNotExists(name, _logger, cancellationToken);
	}

	public async Task CreateTopicAndSubscription(TopicName topicName, SubscriptionName subscription, CancellationToken cancellationToken = default)
	{
		var name = $"{_settings.PrefixName}{topicName.Value}";
		await _settings.CreateTopicAndSubscriptionIfNotExists(name, subscription.Value, _logger, cancellationToken);
	}

	public async Task ClearQueue(QueueName queueName, CancellationToken cancellationToken = default)
	{
		var client = _settings.CreateServiceBusClient();
		var name = $"{_settings.PrefixName}{queueName.Value}";
		var receiver = client.CreateReceiver(name, new ServiceBusReceiverOptions
		{
			PrefetchCount = 0,
			ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
		});

		var secureBreak = 0;
		while (true)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}
			var receiveMessageList = await receiver.ReceiveMessagesAsync(100, TimeSpan.FromMicroseconds(1), cancellationToken);
			if (receiveMessageList == null
				|| !receiveMessageList.Any())
			{
				break;
			}

			if (secureBreak > 10000)
			{
				_logger.LogWarning("Exit from infinite loop for queue {queueName}", queueName.Value);
				break;
			}
		}
	}

	public async Task ClearTopic(TopicName topicName, SubscriptionName subscriptionName, CancellationToken cancellationToken = default)
	{
		var client = _settings.CreateServiceBusClient();
		var name = $"{_settings.PrefixName}{topicName.Value}";
		var receiver = client.CreateReceiver(name, subscriptionName.Value, new ServiceBusReceiverOptions
		{
			PrefetchCount = 0,
			ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
		});

		var secureBreak = 0;
		while (true)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}
			var receiveMessageList = await receiver.ReceiveMessagesAsync(100, TimeSpan.FromMicroseconds(1), cancellationToken);
			if (receiveMessageList == null
				|| !receiveMessageList.Any())
			{
				break;
			}

			if (secureBreak > 10000)
			{
				_logger.LogWarning("Exit from infinite loop for topic {topicName} subscription {subscriptionName}", topicName.Value, subscriptionName.Value);
				break;
			}
		}

	}

	public async Task DeleteQueue(QueueName queueName, CancellationToken cancellationToken = default)
	{
		if (!await IsQueueExists(queueName, cancellationToken))
		{
			_logger.LogWarning("try to delete not existing queue {queueName}", queueName.Value);
			return;
		}
		var managementClient = new ServiceBusAdministrationClient(_settings.BusConnectionString);
		var response = await managementClient.DeleteQueueAsync(queueName.Value, cancellationToken);
		_logger.LogInformation("Delete queue {queueName} with result {statusCode}", queueName, response.Status);
	}

	public async Task DeleteTopic(TopicName topicName, CancellationToken cancellationToken = default)
	{
		if (!await IsTopicExists(topicName, cancellationToken))
		{
			_logger.LogWarning("try to delete not existing topic {topicName}", topicName.Value);
			return;
		}
		var managementClient = new ServiceBusAdministrationClient(_settings.BusConnectionString);
		var response = await managementClient.DeleteTopicAsync(topicName.Value, cancellationToken);
		_logger.LogInformation("Delete topic {topicName} with result {statusCode}", topicName, response.Status);
	}

	public async Task DeleteSubscription(TopicName topicName, SubscriptionName subscriptionName, CancellationToken cancellationToken = default)
	{
		var managementClient = new ServiceBusAdministrationClient(_settings.BusConnectionString);
		var response = await managementClient.DeleteSubscriptionAsync(topicName.Value, subscriptionName.Value, cancellationToken);
		_logger.LogInformation("Delete subscription {subscriptionName} for topic {topicName} with result {statusCode}", subscriptionName, topicName, response.Status);
	}

	public async Task<bool> IsQueueExists(QueueName queueName, CancellationToken cancellationToken = default)
	{
		var managementClient = new ServiceBusAdministrationClient(_settings.BusConnectionString);
		var response = await managementClient.QueueExistsAsync(queueName.Value, cancellationToken);
		return response.Value;
	}

	public async Task<bool> IsTopicExists(TopicName topicName, CancellationToken cancellationToken = default)
	{
		var managementClient = new ServiceBusAdministrationClient(_settings.BusConnectionString);
		var response = await managementClient.TopicExistsAsync(topicName.Value, cancellationToken);
		return response.Value;
	}

	public async Task<bool> IsSubscriptionExists(TopicName topicName, SubscriptionName subscriptionName, CancellationToken cancellationToken = default)
	{
		var managementClient = new ServiceBusAdministrationClient(_settings.BusConnectionString);
		var response = await managementClient.SubscriptionExistsAsync(topicName.Value, subscriptionName.Value, cancellationToken);
		return response.Value;
	}

	public IEnumerable<QueueName> GetRegisteredQueueNameList()
	{
		var result = _settings.QueueReaderList.Select(i => new QueueName(i.QueueName)).ToList();
		return result;
	}

	public IDictionary<TopicName, SubscriptionName> GetRegisteredTopicAndSubscriptionNameList()
	{
		var result = _settings.TopicReaderList.Select(i => new { TopicName = new TopicName(i.TopicName), SubscriptionName = new SubscriptionName(i.SubscriptionName) }).ToList();
		return result.ToDictionary(i => i.TopicName, j => j.SubscriptionName);
	}

}
