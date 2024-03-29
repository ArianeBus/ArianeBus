﻿using ArianeBus.MediatR;

using Microsoft.Extensions.DependencyInjection;

namespace ArianeBus;

public static class StartupExtensions
{
	public static void AddMediatRBus(this IServiceCollection services, string topicName)
	{
		services.AddMediatRBus(config =>
		{
			config.TopicName = topicName;
		});
	}

	public static void AddMediatRBus(this IServiceCollection services, Action<MediatRBusConfiguration> configure)
	{
		var config = new MediatRBusConfiguration();
		configure(config);
		services.AddSingleton(config);
		services.AddMediatR(config =>
		{
			config.RegisterServicesFromAssemblies(typeof(ArianeBus.MediatR.ArianeBusConfig).Assembly);
		});
		services.AddArianeBus(reg =>
		{
			reg.RegisterTopicReader<NotificationReader>(new TopicName(config.TopicName), new SubscriptionName(config.SubscriptionName));
		});
	}
}
