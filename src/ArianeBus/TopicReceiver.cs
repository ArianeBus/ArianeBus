using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArianeBus;

internal class TopicReceiver<T> : ReceiverBase<T>, ITopicReader
{
	public TopicReceiver(ArianeSettings settings,
		ILogger<TopicReceiver<T>> logger,
		IServiceProvider serviceProvider)
		: base(settings, logger, serviceProvider)
	{
	}

	public string SubscriptionName { get; set; } = null!;
	public Type ReaderType { get; set; } = null!;
    public Type MessageType { get; set; } = null!;

    public override async Task StartAsync(CancellationToken cancellationToken)
	{
		await _settings.CreateTopicAndSubscriptionIfNotExists(QueueOrTopicName, SubscriptionName, _logger, cancellationToken);

		_serviceBusClient = _settings.CreateServiceBusClient();

		_serviceBusReceiver = _serviceBusClient.CreateReceiver(QueueOrTopicName, SubscriptionName, new ServiceBusReceiverOptions
		{
			PrefetchCount = 0,
			ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
		});

		try
		{
			_reader = ActivatorUtilities.CreateInstance(_serviceProvider, ReaderType) as MessageReaderBase<T>;
			if (_reader is null)
			{
				_logger.LogCritical("Unable to create instance of {type}", typeof(T).Name);
				throw new InvalidOperationException($"Unable to create instance of {typeof(T).Name}");
			}
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "Unable to create instance of {type}", typeof(T).Name);
			throw new InvalidOperationException($"Unable to create instance of {typeof(T).Name}", ex);
		}
		_reader!.QueueOrTopicName = QueueOrTopicName;
		_reader.FromSubscriptionName = SubscriptionName;

		_logger.LogInformation("TopicReceiver<{type}> started for topic {topicName} with subscription {subscription}", typeof(T).Name, QueueOrTopicName, SubscriptionName);	

		await base.StartAsync(cancellationToken);
	}
}
