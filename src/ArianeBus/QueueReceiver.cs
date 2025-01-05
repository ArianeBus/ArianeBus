using Microsoft.Extensions.DependencyInjection;

namespace ArianeBus;

internal class QueueReceiver<T> : ReceiverBase<T>, IQueueReader
{
	public QueueReceiver(
		ArianeSettings settings,
		ILogger<QueueReceiver<T>> logger,
		IServiceProvider serviceProvider)
		: base(settings, logger, serviceProvider)
	{
	}


	public Type MessageType { get; set; } = default!;
	public Type ReaderType { get; set; } = default!;

	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		await _settings.CreateQueueIfNotExists(QueueOrTopicName, _logger, cancellationToken);

		_serviceBusClient = _settings.CreateServiceBusClient();

		_serviceBusReceiver = _serviceBusClient.CreateReceiver(QueueOrTopicName, new ServiceBusReceiverOptions
		{
			PrefetchCount = 0,
			ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
		});

		_reader = ActivatorUtilities.CreateInstance(_serviceProvider, ReaderType) as MessageReaderBase<T>;
		_reader!.QueueOrTopicName = QueueOrTopicName;

		_logger.LogInformation("QueueReceiver<{Type}> started for queue {Queue}", typeof(T).Name, QueueOrTopicName);

		await base.StartAsync(cancellationToken);
	}


}
