using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ArianeBus;

public static class StartupExtensions
{
	public static void AddArianeBus(this IServiceCollection services, Action<ArianeSettings> config)
	{
		var settings = new ArianeSettings();
		config.Invoke(settings);

		services.AddArianeBus(settings);
	}
	public static void AddArianeBus(this IServiceCollection services, ArianeSettings settings)
	{
		if (settings is null)
		{
			throw new ArgumentNullException(paramName: "settings");
		}
		services.AddSingleton(settings);

		services.AddSingleton<ServiceBuSenderFactory>();
		services.AddSingleton<SendMessageStrategyBase, SendBufferizedMessagesStrategy>();
		services.AddSingleton<SendMessageStrategyBase, SendMessageOneByOneStrategy>();
		services.AddTransient<IServiceBus, ServiceBus>();

		services.AddSingleton(sp =>
		{
			var serviceBusClient = new ServiceBusClient(settings.BusConnectionString, new ServiceBusClientOptions()
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
			services.AddSingleton<IHostedService>(sp =>
			{
				var readerType = topicReader.ReaderType;
				var baseType = readerType.BaseType; // MessageReaderBase<>

				var args = baseType!.GetGenericArguments();
				var messageType = args[0];

				var topicBaseType = typeof(TopicReceiver<>);
				var topicReaderType = topicBaseType.MakeGenericType(messageType);
				var tr = (ITopicReader)ActivatorUtilities.CreateInstance(sp, topicReaderType)!;
				tr.QueueOrTopicName = $"{settings.PrefixName}{topicReader.TopicName}";
				tr.SubscriptionName = topicReader.SubscriptionName;
				tr.MessageType = messageType;
				tr.ReaderType = readerType;
				return (BackgroundService)tr;
			});
		}

		foreach (var queueReader in settings.QueueReaderList)
		{
			services.AddSingleton<IHostedService>(sp =>
			{
				var readerType = queueReader.ReaderType;
				var baseType = readerType.BaseType; // MessageReaderBase<>

				var args = baseType!.GetGenericArguments();
				var messageType = args[0];

				var baseQueueReaderType = typeof(QueueReceiver<>);
				var queueReaderType = baseQueueReaderType.MakeGenericType(messageType);
				var qr = (IQueueReader)ActivatorUtilities.CreateInstance(sp, queueReaderType)!;
				qr.QueueOrTopicName = $"{settings.PrefixName}{queueReader.QueueName}";
				qr.MessageType = messageType;
				qr.ReaderType = readerType;
				return (BackgroundService)qr;
			});
		}
	}
}