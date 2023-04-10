using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace ArianeBus;

public class SendMessageMockStrategy : SendMessageStrategyBase
{
	private readonly ArianeSettings _settings;
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger _logger;

	public SendMessageMockStrategy(ArianeSettings settings,
		IServiceProvider serviceProvider,
		ILogger<SendMessageMockStrategy> logger)
	{
		_settings = settings;
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	public override string StrategyName => "mock";

	public override async Task TrySendRequest(MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		Type? readerType = null;
		if (messageRequest.QueueType == QueueType.Queue)
		{
			var registeredQueue = _settings.QueueReaderList.FirstOrDefault(i => i.QueueName.Equals(messageRequest.QueueOrTopicName, StringComparison.InvariantCultureIgnoreCase));
			if (registeredQueue != null)
			{
				readerType = registeredQueue.ReaderType;
				await SendInternal(messageRequest, readerType, cancellationToken);
			}
			else
			{
				_logger.LogWarning("Queue {queueName} not registered", messageRequest.QueueOrTopicName);
			}
		}
		else if (messageRequest.QueueType == QueueType.Topic)
		{
			var registeredTopicList = _settings.TopicReaderList.Where(i => i.TopicName.Equals(messageRequest.QueueOrTopicName, StringComparison.InvariantCultureIgnoreCase)).ToList();
			if (registeredTopicList != null
				&& registeredTopicList.Any())
			{
				foreach (var item in registeredTopicList)
				{
					readerType = item.ReaderType;
					await SendInternal(messageRequest, readerType, cancellationToken);
				}
			}
			else
			{
				_logger.LogWarning("Topic {topicName} not registered", messageRequest.QueueOrTopicName);
			}
		}
	}

	private async Task SendInternal(MessageRequest messageRequest, Type readerType, CancellationToken cancellationToken)
	{
		var reader = ActivatorUtilities.CreateInstance(_serviceProvider, readerType);
		if (reader != null)
		{
			var methodInfo = reader.GetType().GetMethod("ProcessMessageAsync")!;
			var parameters = new object?[] { messageRequest.Message, cancellationToken };
			if (methodInfo.Invoke(reader, parameters) is not Task task)
			{
				throw new InvalidOperationException("ProcessMessageAsync must return a Task");
			}
			await task!.ConfigureAwait(false);
		}
		else
		{
			_logger.LogWarning("Reader for message {messageType} not instancied", messageRequest.Message.GetType().Name);
		}
	}
}
