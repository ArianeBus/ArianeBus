namespace ArianeBus;

internal class ServiceBus(
	IEnumerable<SendMessageStrategyBase> senderStrategyList,
	ILogger<ServiceBus> logger,
	ArianeSettings settings,
	ServiceBuSenderFactory serviceBuSenderFactory,
	SpeedMessageSender speedMessageSender
	)
	: IServiceBus
{
	public async Task PublishTopic<T>(string topicName, T message, MessageOptions? options = null, CancellationToken cancellationToken = default)
		where T : class
	{
		if (message == null)
		{
			logger.LogWarning("request is null");
			return;
		}

		if (string.IsNullOrWhiteSpace(topicName))
		{
			logger.LogWarning("topicName name is null");
			return;
		}

		var messageRequest = new MessageRequest
		{
			Message = message,
			QueueOrTopicName = $"{settings.PrefixName}{topicName}",
			QueueType = QueueType.Topic,
			MessageOptions = options
		};

		try
		{
			await SendInternal(messageRequest, cancellationToken);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, ex.Message);
		}
	}

	public async Task SpeedPublishTopic<T>(string topicName, T message, CancellationToken cancellationToken = default)
		where T : class
	{
		if (message == null)
		{
			logger.LogWarning("request is null");
			return;
		}

		if (string.IsNullOrWhiteSpace(topicName))
		{
			logger.LogWarning("topicName name is null");
			return;
		}

		var messageRequest = new MessageRequest
		{
			Message = message,
			QueueOrTopicName = $"{settings.PrefixName}{topicName}",
			QueueType = QueueType.Topic,
		};

		await speedMessageSender.SendMessage(messageRequest, message, cancellationToken);
	}

	public async Task EnqueueMessage<T>(string queueName, T message, MessageOptions? options = null, CancellationToken cancellationToken = default)
		where T : class
	{
		if (message == null)
		{
			logger.LogWarning("request is null");
			return;
		}

		if (string.IsNullOrWhiteSpace(queueName))
		{
			logger.LogWarning("queue name is null");
			return;
		}

		var messageRequest = new MessageRequest
		{
			Message = message,
			QueueOrTopicName = $"{settings.PrefixName}{queueName}",
			QueueType = QueueType.Queue,
			MessageOptions = options
		};

		await SendInternal(messageRequest, cancellationToken);
	}

	public async Task SpeedEnqueueMessage<T>(string queueName, T message, CancellationToken cancellationToken = default)
	where T : class
	{
		if (message == null)
		{
			logger.LogWarning("request is null");
			return;
		}

		if (string.IsNullOrWhiteSpace(queueName))
		{
			logger.LogWarning("queue name is null");
			return;
		}

		var messageRequest = new MessageRequest
		{
			Message = message,
			QueueOrTopicName = $"{settings.PrefixName}{queueName}",
			QueueType = QueueType.Queue
		};

		await speedMessageSender.SendMessage(messageRequest, message, cancellationToken);
	}


	public async Task SendAsync<TMessage>(string topicOrQueueName, TMessage message, MessageOptions? options = null, CancellationToken cancellationToken = default)
		where TMessage : class
	{
		if (message == null)
		{
			logger.LogWarning("request is null");
			return;
		}

		if (string.IsNullOrWhiteSpace(topicOrQueueName))
		{
			logger.LogWarning("queue name is null");
			return;
		}

		var messageRequest = new MessageRequest
		{
			Message = message,
			QueueOrTopicName = $"{settings.PrefixName}{topicOrQueueName}",
			QueueType = QueueType.Unknown,
			MessageOptions = options
		};

		await SendInternal(messageRequest, cancellationToken);
	}

	internal async Task SendInternal(MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		var strategyName = settings.SendStrategyName;

		if (settings.MessageSendOptionsList.Any(i => i.Key.Equals(messageRequest.QueueOrTopicName, StringComparison.InvariantCultureIgnoreCase)))
		{
			var queueOrTopicOptions = settings.MessageSendOptionsList[messageRequest.QueueOrTopicName];
			strategyName = queueOrTopicOptions.SendStrategyName;
		}

		var sendStrategy = senderStrategyList.SingleOrDefault(i => i.StrategyName.Equals(strategyName, StringComparison.InvariantCultureIgnoreCase));
		if (sendStrategy is null)
		{
			throw new ArgumentOutOfRangeException($"fail to send message with unknown strategy {strategyName}");
		}

		var sender = await serviceBuSenderFactory.GetSender(messageRequest, cancellationToken);
		try
		{
			await sendStrategy!.TrySendRequest(sender, messageRequest, cancellationToken);
			logger.LogTrace("send {Message} in queue {QueueOrTopicName}", messageRequest.Message, messageRequest.QueueOrTopicName);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, ex.Message);
		}
	}

	public async Task<IEnumerable<TMessage>> ReceiveAsync<TMessage>(QueueName queueName, int messageCount, int timeoutInMillisecond, CancellationToken cancellationToken = default)
	{
		var client = settings.CreateServiceBusClient();
		var name = $"{settings.PrefixName}{queueName.Value}";
		var receiver = client.CreateReceiver(name, new ServiceBusReceiverOptions
		{
			PrefetchCount = 0,
			ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
		});

		return await ReceiveInternalAsync<TMessage>(receiver, messageCount, timeoutInMillisecond, cancellationToken);
	}

	public async Task<IEnumerable<TMessage>> ReceiveAsync<TMessage>(TopicName topicName, SubscriptionName subscription, int messageCount, int timeoutInMillisecond, CancellationToken cancellationToken = default)
	{
		var client = settings.CreateServiceBusClient();
		var name = $"{settings.PrefixName}{topicName.Value}";
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

		IReadOnlyList<ServiceBusReceivedMessage>? receiveMessageList = null;
		while (true)
		{
			try
			{
				receiveMessageList = await receiver.ReceiveMessagesAsync(count, TimeSpan.FromMicroseconds(timeoutInMillisecond), cancellationToken);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, ex.Message);
			}

			if (receiveMessageList is null
				|| !receiveMessageList.Any())
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

		if (receiveMessageList is not null)
		{
			foreach (var receiveMessage in receiveMessageList)
			{
				if (receiveMessage.Body == null)
				{
					logger.LogWarning("receive message with body null on queue {queueName}", receiver.EntityPath);
					continue;
				}

				try
				{
					var bodyContent = receiveMessage.Body.ToString();
					var message = System.Text.Json.JsonSerializer.Deserialize<TMessage>(bodyContent, JsonSerializer.Options)!;
					result.Add(message);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "ErrorMessage : {message}", ex.Message);
				}
			}
		}

		return result;
	}

	public async Task CreateQueue(QueueName queueName, CancellationToken cancellationToken = default)
	{
		var name = $"{settings.PrefixName}{queueName.Value}";
		await settings.CreateQueueIfNotExists(name, logger, cancellationToken);
	}

	public async Task CreateTopic(TopicName topicName, CancellationToken cancellationToken = default)
	{
		var name = $"{settings.PrefixName}{topicName.Value}";
		await settings.CreateTopicIfNotExists(name, logger, cancellationToken);
	}

	public async Task CreateTopicAndSubscription(TopicName topicName, SubscriptionName subscription, CancellationToken cancellationToken = default)
	{
		var name = $"{settings.PrefixName}{topicName.Value}";
		await settings.CreateTopicAndSubscriptionIfNotExists(name, subscription.Value, logger, cancellationToken);
	}

	public async Task ClearQueue(QueueName queueName, CancellationToken cancellationToken = default)
	{
		var client = settings.CreateServiceBusClient();
		var name = $"{settings.PrefixName}{queueName.Value}";
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
				logger.LogWarning("Exit from infinite loop for queue {queueName}", queueName.Value);
				break;
			}
		}
	}

	public async Task ClearTopic(TopicName topicName, SubscriptionName subscriptionName, CancellationToken cancellationToken = default)
	{
		var client = settings.CreateServiceBusClient();
		var name = $"{settings.PrefixName}{topicName.Value}";
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
				logger.LogWarning("Exit from infinite loop for topic {topicName} subscription {subscriptionName}", topicName.Value, subscriptionName.Value);
				break;
			}
		}

	}

	public async Task DeleteQueue(QueueName queueName, CancellationToken cancellationToken = default)
	{
		if (!await IsQueueExists(queueName, cancellationToken))
		{
			logger.LogWarning("try to delete not existing queue {queueName}", queueName.Value);
			return;
		}
		var managementClient = new ServiceBusAdministrationClient(settings.BusConnectionString);
		var response = await managementClient.DeleteQueueAsync(queueName.Value, cancellationToken);
		logger.LogInformation("Delete queue {queueName} with result {statusCode}", queueName, response.Status);
	}

	public async Task DeleteTopic(TopicName topicName, CancellationToken cancellationToken = default)
	{
		if (!await IsTopicExists(topicName, cancellationToken))
		{
			logger.LogWarning("try to delete not existing topic {topicName}", topicName.Value);
			return;
		}
		var managementClient = new ServiceBusAdministrationClient(settings.BusConnectionString);
		var response = await managementClient.DeleteTopicAsync(topicName.Value, cancellationToken);
		logger.LogInformation("Delete topic {topicName} with result {statusCode}", topicName, response.Status);
	}

	public async Task DeleteSubscription(TopicName topicName, SubscriptionName subscriptionName, CancellationToken cancellationToken = default)
	{
		var managementClient = new ServiceBusAdministrationClient(settings.BusConnectionString);
		var response = await managementClient.DeleteSubscriptionAsync(topicName.Value, subscriptionName.Value, cancellationToken);
		logger.LogInformation("Delete subscription {subscriptionName} for topic {topicName} with result {statusCode}", subscriptionName, topicName, response.Status);
	}

	public async Task<bool> IsQueueExists(QueueName queueName, CancellationToken cancellationToken = default)
	{
		var managementClient = new ServiceBusAdministrationClient(settings.BusConnectionString);
		var response = await managementClient.QueueExistsAsync(queueName.Value, cancellationToken);
		return response.Value;
	}

	public async Task<bool> IsTopicExists(TopicName topicName, CancellationToken cancellationToken = default)
	{
		var managementClient = new ServiceBusAdministrationClient(settings.BusConnectionString);
		var response = await managementClient.TopicExistsAsync(topicName.Value, cancellationToken);
		return response.Value;
	}

	public async Task<bool> IsSubscriptionExists(TopicName topicName, SubscriptionName subscriptionName, CancellationToken cancellationToken = default)
	{
		var managementClient = new ServiceBusAdministrationClient(settings.BusConnectionString);
		var response = await managementClient.SubscriptionExistsAsync(topicName.Value, subscriptionName.Value, cancellationToken);
		return response.Value;
	}

	public IEnumerable<QueueName> GetRegisteredQueueNameList()
	{
		var result = settings.ReaderList.Select(i => new QueueName(i.QueueOrTopicName)).ToList();
		return result;
	}

	public IDictionary<TopicName, SubscriptionName> GetRegisteredTopicAndSubscriptionNameList()
	{
		var result = settings.ReaderList.Select(i => new { TopicName = new TopicName(i.QueueOrTopicName), SubscriptionName = new SubscriptionName(i.SubscriptionName) }).ToList();
		return result.ToDictionary(i => i.TopicName, j => j.SubscriptionName);
	}

}
