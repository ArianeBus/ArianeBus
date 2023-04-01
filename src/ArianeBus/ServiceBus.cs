using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Azure.Core;

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

    public Task<IEnumerable<T>> ReceiveAsync<T>(string queueName, int count, int timeoutInMillisecond)
	{
		throw new NotImplementedException();
	}

	public async Task PublishTopic<T>(string topicName, T request, MessageOptions? options = null, CancellationToken? cancellationToken = null)
		where T : class, new()
	{
		if (request == null)
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
			Message = request,
			QueueOrTopicName = topicName,
			QueueType = QueueType.Topic
		};

		await SendInternal(messageRequest, cancellationToken ?? new CancellationTokenSource().Token);
	}

	public async Task EnqueueMessage<T>(string queueName, T request, MessageOptions? options = null, CancellationToken? cancellationToken = null)
		where T : class, new()
	{
		if (request == null)
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
			Message = request,
			QueueOrTopicName = queueName,
			QueueType = QueueType.Queue
		};

		await SendInternal(messageRequest, cancellationToken ?? new CancellationTokenSource().Token);
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
}
