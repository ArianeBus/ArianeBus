using ConsoleWriter;

using ArianeBus;
using ConsoleReader;

IHost host = Host.CreateDefaultBuilder(args)
		.ConfigureAppConfiguration((ctx, builder) =>
		{
			builder.AddJsonFile("appsettings.json", true, false)
				.AddJsonFile("appsettings.local.json", true, false);
		})
		.ConfigureServices((ctx, services) =>
		{
			var cs = ctx.Configuration.GetConnectionString("azurebus");

			services.AddArianeBus(config =>
			{
				config.BusConnectionString = cs!;
				config.RegisterQueueReader<SampleMessageReader>(new QueueName("test1"));
			});

			services.AddLogging();

			services.AddLogging(config =>
			{
				config.SetMinimumLevel(LogLevel.Trace);
				config.AddConsole();
			});
		})
		.Build();

host.Run();