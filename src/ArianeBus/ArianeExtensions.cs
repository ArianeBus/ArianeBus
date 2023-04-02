﻿using Azure.Messaging.ServiceBus;
using System.Runtime;

namespace ArianeBus;

public static class ArianeExtensions
{
	internal static async Task CreateTopicAndSubscriptionIfNotExists(this ArianeSettings settings,
		string topicName,
		string subscriptionName,
		ILogger logger,
		CancellationToken cancellationToken)
	{
		var managementClient = new ServiceBusAdministrationClient(settings.BusConnectionString);
		var topicExists = await managementClient.TopicExistsAsync(topicName, cancellationToken);
		if (!topicExists)
		{
			var topicOptions = new CreateTopicOptions(topicName)
			{
				DefaultMessageTimeToLive = TimeSpan.FromDays(settings.DefaultMessageTimeToLiveInDays),
				AutoDeleteOnIdle = TimeSpan.FromDays(settings.AutoDeleteOnIdleInDays),
				EnableBatchedOperations = true,
			};
			topicOptions.AuthorizationRules.Add(new SharedAccessAuthorizationRule("allClaims"
				, new[] { AccessRights.Manage, AccessRights.Send, AccessRights.Listen }));

			await managementClient.CreateTopicAsync(topicOptions, cancellationToken);

			logger.LogInformation("Azure topic {topicName} created", topicName);
		}

		if (!string.IsNullOrWhiteSpace(subscriptionName))
		{
			var subscriptionExists = await managementClient.SubscriptionExistsAsync(topicName, subscriptionName, cancellationToken);
			if (!subscriptionExists)
			{
				var subscriptionOptions = new CreateSubscriptionOptions(topicName, subscriptionName)
				{
					EnableBatchedOperations = true,
					DefaultMessageTimeToLive = TimeSpan.FromDays(settings.DefaultMessageTimeToLiveInDays),
					AutoDeleteOnIdle = TimeSpan.FromDays(settings.AutoDeleteOnIdleInDays)
				};

				await managementClient.CreateSubscriptionAsync(subscriptionOptions, cancellationToken);

				logger.LogInformation("Azure subscription {subscriptionName} created for topic {topicName}", subscriptionName, topicName);
			}
		}
	}

	internal static async Task CreateQueueIfNotExists(this ArianeSettings settings,
		string queueName,
		ILogger logger,
		CancellationToken cancellationToken)
	{
		var managementClient = new ServiceBusAdministrationClient(settings.BusConnectionString);
		var queueExists = await managementClient.QueueExistsAsync(queueName, cancellationToken);
		if (!queueExists)
		{
			var options = new CreateQueueOptions(queueName)
			{
				DefaultMessageTimeToLive = TimeSpan.FromDays(settings.DefaultMessageTimeToLiveInDays),
				AutoDeleteOnIdle = TimeSpan.FromDays(settings.AutoDeleteOnIdleInDays),
				EnableBatchedOperations = true,
				MaxDeliveryCount = settings.MaxDeliveryCount,
			};
			options.AuthorizationRules.Add(new SharedAccessAuthorizationRule("allClaims"
				, new[] { AccessRights.Manage, AccessRights.Send, AccessRights.Listen }));

			await managementClient.CreateQueueAsync(options, cancellationToken);

			logger.LogInformation("Azure queue {queueName} created", queueName);
		}
	}

	internal static ServiceBusClient CreateServiceBusClient(this ArianeSettings settings)
	{
		var client = new ServiceBusClient(settings.BusConnectionString, new ServiceBusClientOptions
		{
			TransportType = ServiceBusTransportType.AmqpTcp,
			RetryOptions = new ServiceBusRetryOptions()
			{
				Mode = ServiceBusRetryMode.Exponential,
				MaxRetries = 3,
				MaxDelay = TimeSpan.FromSeconds(10)
			}
		});

		return client;
	}

	public static void RegisterTopicReader<TReader>(this ArianeSettings settings, TopicName topicName, SubscriptionName subscriptionName)
		where TReader : IMessageReader
	{
		if (settings.TopicReaderList.Any(i => i.TopicName.Equals(topicName.Value, StringComparison.InvariantCultureIgnoreCase)
												&& i.SubscriptionName.Equals(subscriptionName.Value, StringComparison.InvariantCultureIgnoreCase)))
		{
			return;
		}
		var topicReader = new TopicReaderRegistration
		{
			TopicName = topicName.Value,
			SubscriptionName = subscriptionName.Value,
			ReaderType = typeof(TReader)
		};
		settings.TopicReaderList.Add(topicReader);
	}

	public static void RegisterQueueReader<TReader>(this ArianeSettings settings, QueueName queueName)
		where TReader : IMessageReader
	{
		if (settings.QueueReaderList.Any(i => i.QueueName.Equals(queueName.Value, StringComparison.InvariantCultureIgnoreCase)))
		{
			return;
		}
		var queueReader = new QueueReaderRegistration
		{
			QueueName = queueName.Value,
			ReaderType = typeof(TReader)
		};
		settings.QueueReaderList.Add(queueReader);
	}
}
