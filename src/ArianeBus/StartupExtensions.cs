using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ArianeBus;

public static class StartupExtensions
{
	public static void AddArianeBus(this IServiceCollection services, Action<ArianeSettings> config)
	{
		var defaultSettings = new ArianeSettings();
		config.Invoke(defaultSettings);

		services.AddArianeBus(defaultSettings);
	}

	/// <summary>
	/// Add ArianeBus to the service collection with the specified settings object and the default implementation of the <see cref="SendMessageStrategyBase"/> class.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="settings"></param>
	/// <exception cref="ArgumentNullException"></exception>
	public static void AddArianeBus(this IServiceCollection services, ArianeSettings settings)
	{
		if (settings is null)
		{
			throw new ArgumentNullException(nameof(settings));
		}

		ArianeSettings? arianeSettings;

		var registeredSettings = services.Where(i => i.ServiceType == typeof(ArianeSettings)).FirstOrDefault();
		if (registeredSettings is null)
		{
			arianeSettings = new ArianeSettings();
			services.TryAddSingleton(arianeSettings);
		}
		else
		{
			arianeSettings = (ArianeSettings)registeredSettings.ImplementationInstance!;
		}

		var defaultSettings = new ArianeSettings();

		if (!string.IsNullOrWhiteSpace(settings.BusConnectionString))
		{
			arianeSettings.BusConnectionString = settings.BusConnectionString;
		}
		if (settings.AutoDeleteOnIdleInDays != defaultSettings.AutoDeleteOnIdleInDays)
		{
			arianeSettings.AutoDeleteOnIdleInDays = settings.AutoDeleteOnIdleInDays;
		}
		if (settings.DefaultMessageTimeToLiveInDays != defaultSettings.DefaultMessageTimeToLiveInDays)
		{
			arianeSettings.DefaultMessageTimeToLiveInDays = settings.DefaultMessageTimeToLiveInDays;
		}
		if (settings.CreateQueueIfNotExists != defaultSettings.CreateQueueIfNotExists)
		{
			arianeSettings.CreateQueueIfNotExists = settings.CreateQueueIfNotExists;
		}
		if (settings.BatchSendingBufferSize != defaultSettings.BatchSendingBufferSize)
		{
			arianeSettings.BatchSendingBufferSize = settings.BatchSendingBufferSize;
		}
		if (settings.CreateSubscriptionIfNotExists != defaultSettings.CreateSubscriptionIfNotExists)
		{
			arianeSettings.CreateSubscriptionIfNotExists = settings.CreateSubscriptionIfNotExists;
		}
		if (settings.MaxDeliveryCount != defaultSettings.MaxDeliveryCount)
		{
			arianeSettings.MaxDeliveryCount = settings.MaxDeliveryCount;
		}
		if (settings.PrefixName != defaultSettings.PrefixName)
		{
			arianeSettings.PrefixName = settings.PrefixName;
		}
		if (settings.ReceiveMessageBufferSize != defaultSettings.ReceiveMessageBufferSize)
		{
			arianeSettings.ReceiveMessageBufferSize = settings.ReceiveMessageBufferSize;
		}
		if (settings.ReceiveMessageTimeoutInSecond != defaultSettings.ReceiveMessageTimeoutInSecond)
		{
			arianeSettings.ReceiveMessageTimeoutInSecond = settings.ReceiveMessageTimeoutInSecond;
		}
		if (settings.UseMockForUnitTests != defaultSettings.UseMockForUnitTests)
		{
			arianeSettings.UseMockForUnitTests = settings.UseMockForUnitTests;
		}

		services.TryAddSingleton<ServiceBuSenderFactory>();
		if (settings.UseMockForUnitTests)
		{
			arianeSettings.SendStrategyName = "mock";
			services.TryAddSingleton<SendMessageStrategyBase, SendMessageMockStrategy>();
		}
		else
		{
			services.TryAddSingleton<SendMessageStrategyBase, SendBufferizedMessagesStrategy>();
			services.TryAddSingleton<SendMessageStrategyBase, SendMessageOneByOneStrategy>();
		}
		services.TryAddTransient<IServiceBus, ServiceBus>();

		services.TryAddSingleton(sp =>
		{
			var serviceBusClient = new ServiceBusClient(arianeSettings.BusConnectionString, new ServiceBusClientOptions()
			{
				TransportType = ServiceBusTransportType.AmqpTcp,
				RetryOptions = new ServiceBusRetryOptions()
				{
					Mode = ServiceBusRetryMode.Exponential,
					MaxRetries = 3,
					MaxDelay = TimeSpan.FromSeconds(10)
				}
			});
			return serviceBusClient;
		});

		foreach (var topicReader in settings.TopicReaderList)
		{
			arianeSettings.RegisterTopicReader(topicReader);
		}
		foreach (var topicReader in arianeSettings.TopicReaderList)
		{
			services.AddSingleton<IHostedService>(sp =>
			{
				var readerType = topicReader.ReaderType;
				var baseType = readerType.BaseType; // MessageReaderBase<>

				var args = baseType!.GetGenericArguments();
				var messageType = args[0];

				var topicBaseType = typeof(TopicReceiver<>);
				var topicReaderType = topicBaseType.MakeGenericType(messageType);
				var tr = (ITopicReader)ActivatorUtilities.CreateInstance(sp, topicReaderType)!;
				tr.QueueOrTopicName = $"{arianeSettings.PrefixName}{topicReader.TopicName}";
				tr.SubscriptionName = topicReader.SubscriptionName;
				tr.MessageType = messageType;
				tr.ReaderType = readerType;
				return (BackgroundService)tr;
			});
		}

		foreach (var queueReader in settings.QueueReaderList)
		{
			arianeSettings.RegisterQueueReader(queueReader);
		}
		foreach (var queueReader in arianeSettings.QueueReaderList)
		{
			services.TryAddSingleton<IHostedService>(sp =>
			{
				var readerType = queueReader.ReaderType;
				var baseType = readerType.BaseType; // MessageReaderBase<>

				var args = baseType!.GetGenericArguments();
				var messageType = args[0];

				var baseQueueReaderType = typeof(QueueReceiver<>);
				var queueReaderType = baseQueueReaderType.MakeGenericType(messageType);
				var qr = (IQueueReader)ActivatorUtilities.CreateInstance(sp, queueReaderType)!;
				qr.QueueOrTopicName = $"{arianeSettings.PrefixName}{queueReader.QueueName}";
				qr.MessageType = messageType;
				qr.ReaderType = readerType;
				return (BackgroundService)qr;
			});
		}
	}
}