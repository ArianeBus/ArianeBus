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
			var registeredStrategies = services.Where(i => i.ServiceType == typeof(SendMessageStrategyBase));
			if (!registeredStrategies.Any(i => i.ImplementationType == typeof(SendBufferizedMessagesStrategy)))
			{
				services.AddSingleton<SendMessageStrategyBase, SendBufferizedMessagesStrategy>();
			}
			if (!registeredStrategies.Any(i => i.ImplementationType == typeof(SendMessageOneByOneStrategy)))
			{
				services.AddSingleton<SendMessageStrategyBase, SendMessageOneByOneStrategy>();
			}
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

		foreach (var reader in settings.ReaderList)
		{
			if (reader.QueueType == QueueType.Queue)
			{
                arianeSettings.RegisterQueueReader(reader);
            }
            else if (reader.QueueType == QueueType.Topic)
			{
                arianeSettings.RegisterTopicReader(reader);
            }
        }

		foreach (var reader in arianeSettings.ReaderList)
		{
			if (reader.IsRegistered)
			{
				continue;
			}
			reader.IsRegistered = true;
			services.AddSingleton<IHostedService>(sp =>
			{
				var readerType = reader.ReaderType;
				var baseType = readerType.BaseType; // MessageReaderBase<>

				var args = baseType!.GetGenericArguments();
				var messageType = args[0];

				if (reader.QueueType == QueueType.Topic)
				{
					var topicBaseType = typeof(TopicReceiver<>);
					var topicReaderType = topicBaseType.MakeGenericType(messageType);

					var tr = (ITopicReader)ActivatorUtilities.GetServiceOrCreateInstance(sp, topicReaderType)!;
					tr.QueueOrTopicName = $"{arianeSettings.PrefixName}{reader.QueueOrTopicName}";
					tr.SubscriptionName = reader.SubscriptionName;
					tr.MessageType = messageType;
					tr.ReaderType = readerType;
					return (BackgroundService)tr;
				}
				else if (reader.QueueType == QueueType.Queue)
				{
					var baseQueueReaderType = typeof(QueueReceiver<>);
					var queueReaderType = baseQueueReaderType.MakeGenericType(messageType);

					var qr = (IQueueReader)ActivatorUtilities.GetServiceOrCreateInstance(sp, queueReaderType)!;
					qr.QueueOrTopicName = $"{arianeSettings.PrefixName}{reader.QueueOrTopicName}";
					qr.MessageType = messageType;
					qr.ReaderType = readerType;
					return (BackgroundService)qr;
				}
				return default!;
			});
		}

		foreach (var messageOption in settings.MessageSendOptionsList)
		{
			arianeSettings.RegisterQueueOrTopicBehaviorOptions(messageOption.Key, messageOption.Value);
		}
	}
}