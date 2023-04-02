using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus;

public abstract class ReceiverBase<T> : BackgroundService
{
	protected ServiceBusReceiver? _serviceBusReceiver;
	protected ServiceBusClient? _serviceBusClient;
	protected MessageReaderBase<T>? _reader;

	protected readonly ArianeSettings _settings;
	protected readonly ILogger _logger;
	protected readonly IServiceProvider _serviceProvider;

	protected ReceiverBase(ArianeSettings settings,
		ILogger<ReceiverBase<T>> logger,
		IServiceProvider serviceProvider)
	{
		_settings = settings;
		_logger = logger;
		_serviceProvider = serviceProvider;
	}

	public string QueueOrTopicName { get; set; } = null!;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			if (_serviceBusClient!.IsClosed)
			{
				break;
			}
			var receiveMessageList = await _serviceBusReceiver!.ReceiveMessagesAsync(_settings.ReceiveMessageBufferSize, TimeSpan.FromSeconds(_settings.ReceiveMessageTimeoutInSecond), stoppingToken);
			if (receiveMessageList == null
				|| !receiveMessageList.Any())
			{
				continue;
			}

			foreach (var receiveMessage in receiveMessageList)
			{
				await ProcessMessageAsync(receiveMessage, stoppingToken);
			}
		}
	}

	protected async Task ProcessMessageAsync(ServiceBusReceivedMessage receiveMessage, CancellationToken cancellationToken)
	{
		if (receiveMessage.Body == null)
		{
			_logger.LogWarning("receive message with body null on queue or topic {name}", QueueOrTopicName);
			return;
		}

		try
		{
			var message = System.Text.Json.JsonSerializer.Deserialize<T>(receiveMessage.Body.ToString())!;
			await _reader!.ProcessMessageAsync(message, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "ErrorMessage : {message}", ex.Message);
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		await _serviceBusClient!.DisposeAsync();
		await _serviceBusReceiver!.DisposeAsync();
		await base.StopAsync(cancellationToken);
	}
}
