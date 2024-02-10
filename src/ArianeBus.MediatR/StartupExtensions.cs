using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

namespace ArianeBus.MediatR;

public static class StartupExtensions
{
	public static void AddMediatRBus(this IServiceCollection services, string topicName)
	{
		var config = new MediatRBusConfiguration { TopicName = topicName };
		services.AddSingleton(config);
		services.AddMediatR(config =>
		{
			config.RegisterServicesFromAssemblies(typeof(StartupExtensions).Assembly);
		});
		services.AddArianeBus(services =>
		{
			services.RegisterTopicReader<NotificationReader>(new TopicName(topicName), new SubscriptionName(config.SubscriptionName));
		});
	}
}
