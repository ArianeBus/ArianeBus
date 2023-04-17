using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace ArianeBus.Tests;

internal class RootTest
{
	public static IHost CreateHost(Action<ArianeSettings>? arianeConfig = null, Action<IServiceCollection>? otherServices = null)
	{
		IHost host = Host.CreateDefaultBuilder()
					.ConfigureAppConfiguration((ctx, builder) =>
					{
						builder.AddJsonFile("appSettings.json", true, false)
							.AddJsonFile("appSettings.local.json", true, false);
					})
					.ConfigureServices((ctx, services) =>
					{
						var cs = ctx.Configuration.GetConnectionString("azurebus");

						var settings = new ArianeSettings();
						settings.BusConnectionString = cs!;
						settings.SendStrategyName = $"{ArianeBus.SendStrategy.Bufferized}";
						arianeConfig?.Invoke(settings);

						services.AddArianeBus(settings);

						services.AddLogging();

						services.AddLogging(config =>
						{
							config.SetMinimumLevel(LogLevel.Trace);
							config.AddConsole();
						});

						services.AddSingleton<MessageCollector>();

						otherServices?.Invoke(services);
					})
					.Build();

		return host;
	}
}
