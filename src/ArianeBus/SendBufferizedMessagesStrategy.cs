using System.Collections.Concurrent;

namespace ArianeBus;

internal class SendBufferizedMessagesStrategy : SendMessageStrategyBase
{
	private readonly ConcurrentDictionary<string, MessageBuffer> _messageBuffers = new();
	private readonly ArianeSettings _settings;
	private readonly ILogger _logger;
	private readonly ConcurrentQueue<SendAction> _queue = new();
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public SendBufferizedMessagesStrategy(
		ArianeSettings settings,
		ILogger<SendBufferizedMessagesStrategy> logger)
	{
		_settings = settings;
		_logger = logger;
	}

	public override string StrategyName => $"{SendStrategy.Bufferized}";
	private bool IsProcessing => _semaphore.CurrentCount < 1;

	public override async Task TrySendRequest(ServiceBusSender sender, MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		_messageBuffers.TryGetValue(sender.Identifier, out MessageBuffer? buffer);
		if (buffer is null)
		{
			buffer = new MessageBuffer
			{
				Batch = await sender.CreateMessageBatchAsync(cancellationToken),
				OnTimeout = (batch, b) =>
				{
					SendInternal(sender, batch);
					b.IsProcessed = true;
				}
			};
			_messageBuffers.TryAdd(sender.Identifier, buffer);
		}

		Add(new SendAction
		{
			Sender = sender,
			Action = async () =>
			{
				var busMessage = CreateServiceBusMessage(messageRequest);
				_messageBuffers.TryGetValue(sender.Identifier, out var buffer);
				if (buffer is null)
				{
					_logger.LogWarning("sent message fail");
					return;
				}
				var retryCount = 0;
				while (true)
				{
					try
					{
						buffer!.Batch.TryAddMessage(busMessage);
						break;
					}
					catch (Exception ex)
					{
						if (retryCount > 2)
						{
							_logger.LogError(ex, ex.Message);
							break;
						}
						retryCount++;
						await Task.Delay(1 * 1000); // Waiting for peace...
					}
				}
			}
		});

	}

	private void Add(SendAction action)
	{
		_messageAddedCount++;
		_queue.Enqueue(action);
		if (IsProcessing)
		{
			return;
		}
		InvokeAction();
	}

	private void InvokeAction()
	{
		SendAction? sendAction;
		while (_queue.TryDequeue(out sendAction))
		{
			InternalInvokeAction(sendAction!);
			if (_messageBuffers.TryGetValue(sendAction!.Sender.Identifier, out var currentBuffer)
				&& currentBuffer.Batch.Count >= _settings.BatchSendingBufferSize)
			{
				_logger.LogTrace("batch buffer full for {FullyQualifiedNamespace}", sendAction.Sender.FullyQualifiedNamespace);
				break;
			}
		}

		if (sendAction is not null
			&& _messageBuffers.TryGetValue(sendAction.Sender.Identifier, out var buffer))
		{
			SendInternal(sendAction.Sender, buffer.Batch);
			buffer.IsProcessed = true;
		}
	}

	private void SendInternal(ServiceBusSender sender, ServiceBusMessageBatch batch)
	{
		if (batch.Count == 0)
		{
			return;
		}
		try
		{
			var completed = sender.SendMessagesAsync(batch).Wait(TimeSpan.FromSeconds(15));
			if (!completed)
			{
				_logger.LogWarning("{batchCount} message maybe sent fail for {FullyQualifiedNamespace}", batch.Count, sender.FullyQualifiedNamespace);
			}
			else
			{
				_logger.LogTrace("{batchCount} messages sent in {queueName}", batch.Count, sender.EntityPath);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "{batchCount} messages sent fail for {FullyQualifiedNamespace}", batch.Count, sender.FullyQualifiedNamespace);
		}
		if (_messageBuffers.TryRemove(sender.Identifier, out var byebye))
		{
			byebye.Dispose();
		}
		_messageSentCount += batch.Count;
	}

	private void InternalInvokeAction(SendAction action)
	{
		_semaphore.Wait();
		try
		{
			action.Action();
			_messageProcessedCount++;
		}
		finally
		{
			_semaphore.Release();
		}
		InvokeAction();
	}

}
