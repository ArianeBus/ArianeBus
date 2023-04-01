using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArianeBus;

internal class QueueReader<T> : BackgroundService, IQueueReader
{
	private ServiceBusReceiver? _serviceBusReceiver;
	private ServiceBusClient? _serviceBusClient;
	private MessageReaderBase<T>? _reader;

	private readonly ArianeSettings _settings;
	private readonly ILogger _logger;
	private readonly IServiceProvider _serviceProvider;

	public QueueReader(
		ArianeSettings settings,
		ILogger<QueueReader<T>> logger,
		IServiceProvider serviceProvider)
	{
		_settings = settings;
		_logger = logger;
		_serviceProvider = serviceProvider;
	}

	public string QueueName { get; set; } = null!;
	public Type MessageType { get; set; } = default!;
	public Type ReaderType { get; set; } = default!;

	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		await _settings.CreateQueueIfNotExists(QueueName, _logger, cancellationToken);

		_serviceBusClient = new ServiceBusClient(_settings.BusConnectionString, new ServiceBusClientOptions
		{
			TransportType = ServiceBusTransportType.AmqpTcp,
			RetryOptions = new ServiceBusRetryOptions()
			{
				Mode = ServiceBusRetryMode.Exponential,
				MaxRetries = 3,
				MaxDelay = TimeSpan.FromSeconds(10)
			}
		});

		_serviceBusReceiver = _serviceBusClient.CreateReceiver(QueueName, new ServiceBusReceiverOptions
		{
			PrefetchCount = 0,
			ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
		});

		_reader = ActivatorUtilities.CreateInstance(_serviceProvider, ReaderType) as MessageReaderBase<T>;
		_reader!.QueueOrTopicName = QueueName;

		await base.StartAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			if (_serviceBusClient!.IsClosed)
			{
				break;
			}
			var receiveMessageList = await _serviceBusReceiver!.ReceiveMessagesAsync(10, TimeSpan.FromSeconds(1), stoppingToken);
			if (receiveMessageList == null
				|| !receiveMessageList.Any())
			{
				continue;
			}

			foreach (var receiveMessage in receiveMessageList)
			{
				if (receiveMessage.Body == null)
				{
					_logger.LogWarning("receive message with body null on queue {queueName}", QueueName);
					continue;
				}

				try
				{
					var message = System.Text.Json.JsonSerializer.Deserialize<T>(receiveMessage.Body.ToString())!;
					await _reader!.ProcessMessageAsync(message, stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "ErrorMessage : {message}", ex.Message);
				}
			}
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		await _serviceBusClient!.DisposeAsync();
		await _serviceBusReceiver!.DisposeAsync();
		await base.StopAsync(cancellationToken);
	}
}
