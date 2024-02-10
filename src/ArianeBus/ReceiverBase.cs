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

	public override Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Start queue or topic {name} in background service", QueueOrTopicName);
		return base.StartAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			if (_serviceBusClient!.IsClosed)
			{
				break;
			}
			if (_serviceBusReceiver!.IsClosed)
			{
				break;
			}
			IReadOnlyList<ServiceBusReceivedMessage>? receiveMessageList = null;
			try
			{
				receiveMessageList = await _serviceBusReceiver!.ReceiveMessagesAsync(_settings.ReceiveMessageBufferSize, TimeSpan.FromSeconds(_settings.ReceiveMessageTimeoutInSecond), stoppingToken);
				if (receiveMessageList == null
					|| !receiveMessageList.Any())
				{
					continue;
				}
				_logger.LogTrace("receive {count} messages on queue or topic {name}", receiveMessageList.Count, QueueOrTopicName);
			}
			catch (Exception ex)
			{
				ex.Data["QueueOrTopicName"] = QueueOrTopicName;
				_logger.LogError(ex, ex.Message);
				break;
			}

			if (receiveMessageList is not null)
			{
				foreach (var receiveMessage in receiveMessageList)
				{
					await ProcessMessageAsync(receiveMessage, stoppingToken);
				}
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
			var bodyContent = receiveMessage.Body.ToString();
			var message = System.Text.Json.JsonSerializer.Deserialize<T>(bodyContent, JsonSerializer.Options)!;
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
