﻿using Microsoft.Extensions.DependencyInjection;
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

		_reader = ActivatorUtilities.CreateInstance(_serviceProvider, ReaderType) as MessageReaderBase<T>;
		_reader!.QueueOrTopicName = QueueOrTopicName;
		_reader.FromSubscriptionName = SubscriptionName;

		await base.StartAsync(cancellationToken);
	}
}
