using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArianeBus;

internal class SendBufferizedMessagesStrategy : SendMessageStrategyBase
{
	private readonly ConcurrentDictionary<string, MessageBuffer> _messageBuffers = new();
	private readonly ServiceBuSenderFactory _serviceBuSenderFactory;
	private readonly ArianeSettings _settings;
	private readonly ILogger _logger;
	private readonly ConcurrentQueue<SendAction> _queue = new();
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public SendBufferizedMessagesStrategy(ServiceBuSenderFactory serviceBuSenderFactory,
		ArianeSettings settings,
		ILogger<SendBufferizedMessagesStrategy> logger)
    {
		_serviceBuSenderFactory = serviceBuSenderFactory;
		_settings = settings;
		_logger = logger;
	}

    public override string StrategyName => $"{SendStrategy.Bufferized}";
	private bool IsProcessing => _semaphore.CurrentCount < 1;

	internal override async Task TrySendRequest(MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		var sender = await _serviceBuSenderFactory.GetSender(messageRequest, cancellationToken);
		_messageBuffers.TryGetValue(sender.Identifier, out MessageBuffer? buffer);
		if (buffer is null)
		{
			buffer = new MessageBuffer
			{
				Batch = await sender.CreateMessageBatchAsync(cancellationToken)
			};
			buffer.OnTimeout = async (batch) =>
			{
				await sender.SendMessagesAsync(batch, cancellationToken);
				_messageBuffers.TryRemove(sender.Identifier, out var byebye);
				_messageSentCount += buffer.Batch.Count;
			};
			_messageBuffers.TryAdd(sender.Identifier, buffer);
		}

		Add(new SendAction
		{
			Sender = sender,
			Action = () =>
			{
				var busMessage = CreateServiceBusMessage(messageRequest);
				_messageBuffers.TryGetValue(sender.Identifier, out var buffer);
				if (buffer is null)
				{
					_logger.LogWarning("sent message fail");
					return;
				}
				buffer!.Batch.TryAddMessage(busMessage);
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
			&& _messageBuffers.TryGetValue(sendAction.Sender.Identifier, out var buffer)
			&& buffer.Batch.Count > 0)
		{
			sendAction.Sender.SendMessagesAsync(buffer.Batch).Wait(); // <- J'ai pas mieux :(
			buffer.IsProcessed = true;
			_logger.LogTrace("Sent {batchCount} messages in {FullyQualifiedNamespace}", buffer.Batch.Count, sendAction.Sender.FullyQualifiedNamespace);
			_messageBuffers.TryRemove(sendAction.Sender.Identifier, out var byebye);
			_messageSentCount += buffer.Batch.Count;
		}
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
