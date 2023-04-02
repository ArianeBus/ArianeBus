using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Azure.Core;
using Azure.Messaging.ServiceBus;

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
			QueueType = QueueType.Topic
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
			QueueType = QueueType.Queue
		};

		await SendInternal(messageRequest, cancellationToken);
	}

	internal async Task SendInternal(MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		var sendStrategy = _senderStrategyList.Single(i => i.StrategyName.Equals(_settings.SendStrategyName, StringComparison.InvariantCultureIgnoreCase));
		if (sendStrategy is null)
		{
			_logger.LogWarning("try to send message with unknown strategy {name}", _settings.SendStrategyName);
		}

		await sendStrategy!.TrySendRequest(messageRequest, cancellationToken);
		_logger.LogTrace("send {Message} in queue {QueueOrTopicName}", messageRequest.Message, messageRequest.QueueOrTopicName);
	}

	public async Task<IEnumerable<TMessage>> ReceiveAsync<TMessage>(QueueName queueName, int count, int timeoutInMillisecond, CancellationToken cancellationToken = default)
	{
		var client = _settings.CreateServiceBusClient();
		var name = $"{_settings.PrefixName}{queueName.Value}";
		var receiver = client.CreateReceiver(name, new ServiceBusReceiverOptions
		{
			PrefetchCount = 0,
			ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
		});

		return await ReceiveInternalAsync<TMessage>(receiver, count, timeoutInMillisecond, cancellationToken);
	}

	public async Task<IEnumerable<TMessage>> ReceiveAsync<TMessage>(TopicName topicName, SubscriptionName subscription, int count, int timeoutInMillisecond, CancellationToken cancellationToken = default)
	{
		var client = _settings.CreateServiceBusClient();
		var name = $"{_settings.PrefixName}{topicName.Value}";
		var receiver = client.CreateReceiver(name, subscription.Value, new ServiceBusReceiverOptions
		{
			PrefetchCount = 0,
			ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
		});

		return await ReceiveInternalAsync<TMessage>(receiver, count, timeoutInMillisecond, cancellationToken);
	}

	private async Task<IEnumerable<TMessage>> ReceiveInternalAsync<TMessage>(ServiceBusReceiver receiver, int count, int timeoutInMillisecond, CancellationToken cancellationToken)
	{
		var result = new List<TMessage>();
		var receiveMessageList = await receiver.ReceiveMessagesAsync(count, TimeSpan.FromMicroseconds(timeoutInMillisecond), cancellationToken);
		if (receiveMessageList == null
			|| !receiveMessageList.Any())
		{
			return result;
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

	public async Task CreateTopic(TopicName topicName, SubscriptionName subscription, CancellationToken cancellationToken = default)
	{
		var name = $"{_settings.PrefixName}{topicName.Value}";
		await _settings.CreateTopicAndSubscriptionIfNotExists(name, subscription.Value, _logger, cancellationToken);
	}
}
