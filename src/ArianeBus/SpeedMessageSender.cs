using System.Net.Http.Json;

using Microsoft.Extensions.DependencyInjection;

namespace ArianeBus;

internal class SpeedMessageSender(
	IEnumerable<SendMessageStrategyBase> senderStrategyList,
	IHttpClientFactory httpClientFactory,
	ArianeSettings settings,
	ILogger<SpeedMessageSender> logger,
	IServiceScopeFactory serviceScopeFactory
	)
{
	public async Task SendMessage(MessageRequest messageRequest, object message, CancellationToken cancellationToken)
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

		if ("mock".Equals(strategyName, StringComparison.InvariantCultureIgnoreCase))
		{
			await TrySendRequestMock(messageRequest, cancellationToken);
		}
		else
		{
			await SendInternal(messageRequest, message, cancellationToken);
		}
	}

	private async Task TrySendRequestMock(MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		Type? readerType = null;
		if (messageRequest.QueueType == QueueType.Queue)
		{
			var registeredQueue = settings.ReaderList.FirstOrDefault(i => i.QueueOrTopicName.Equals(messageRequest.QueueOrTopicName, StringComparison.InvariantCultureIgnoreCase));
			if (registeredQueue != null)
			{
				readerType = registeredQueue.ReaderType;
				await SendMockInternal(messageRequest, readerType, cancellationToken);
			}
			else
			{
				logger.LogWarning("Queue {QueueName} not registered", messageRequest.QueueOrTopicName);
			}
		}
		else if (messageRequest.QueueType == QueueType.Topic)
		{
			var registeredTopicList = settings.ReaderList.Where(i => i.QueueOrTopicName.Equals(messageRequest.QueueOrTopicName, StringComparison.InvariantCultureIgnoreCase)).ToList();
			if (registeredTopicList != null
				&& registeredTopicList.Any())
			{
				foreach (var item in registeredTopicList)
				{
					readerType = item.ReaderType;
					await SendMockInternal(messageRequest, readerType, cancellationToken);
				}
			}
			else
			{
				logger.LogWarning("Topic {TopicName} not registered", messageRequest.QueueOrTopicName);
			}
		}
	}

	private async Task SendInternal(MessageRequest messageRequest, object message, CancellationToken cancellationToken)
	{
		var queueName = messageRequest.QueueOrTopicName;

		var cs = settings.AzureBusConnectionString;
		string url = $"https://{cs.Namespace}.servicebus.windows.net/{queueName}/messages".ToLower();

		var client = httpClientFactory.CreateClient("AzureBus");
		var response = await client.PostAsJsonAsync(url, message, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			var detail = await response.Content.ReadAsStringAsync(cancellationToken);
			logger.LogError("Erreur lors de l'envoi du message. {Statut} {Detail}", response.StatusCode, detail);
		}
	}

	private async Task SendMockInternal(MessageRequest messageRequest, Type readerType, CancellationToken cancellationToken)
	{
		var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;
		var reader = ActivatorUtilities.CreateInstance(serviceProvider, readerType);
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
			logger.LogWarning("Reader for message {MessageType} not instancied", messageRequest.Message.GetType().Name);
		}
	}
}
