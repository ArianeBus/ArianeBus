using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArianeBus;

internal class TopicReader<T> : BackgroundService, ITopicReader
{
	private ServiceBusReceiver? _serviceBusReceiver;
	private ServiceBusClient? _serviceBusClient;
	private MessageReaderBase<T>? _reader;

	private readonly ArianeSettings _settings;
	private readonly ILogger _logger;
	private readonly IServiceProvider _serviceProvider;

	public TopicReader(ArianeSettings settings,
		ILogger<TopicReader<T>> logger,
		IServiceProvider serviceProvider)
	{
		_settings = settings;
		_logger = logger;
		_serviceProvider = serviceProvider;
	}

	public string TopicName { get; set; } = null!;
	public string SubscriptionName { get; set; } = null!;
    public Type ReaderType { get; set; }
    public Type MessageType { get; set; } = null!;

    public override async Task StartAsync(CancellationToken cancellationToken)
	{
		await _settings.CreateTopicAndSubscriptionIfNotExists(TopicName, SubscriptionName, _logger, cancellationToken);

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

		_serviceBusReceiver = _serviceBusClient.CreateReceiver(TopicName, SubscriptionName, new ServiceBusReceiverOptions
		{
			PrefetchCount = 0,
			ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
		});

		_reader = ActivatorUtilities.CreateInstance(_serviceProvider, ReaderType) as MessageReaderBase<T>;
		_reader!.QueueOrTopicName = TopicName;
		_reader.FromSubscriptionName = SubscriptionName;

		await base.StartAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var receiveMessage = await _serviceBusReceiver!.ReceiveMessageAsync(TimeSpan.FromSeconds(1), stoppingToken);

			if (receiveMessage != null
				&& receiveMessage.Body != null)
			{
				try
				{
					var message = System.Text.Json.JsonSerializer.Deserialize<T>(receiveMessage.Body.ToString())!;
					await _reader!.ProcessMessageAsync(message, stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, ex.Message);
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
