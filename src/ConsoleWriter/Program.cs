
using System.Diagnostics;

using ArianeBus;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json", true, false)
	.AddJsonFile("appsettings.local.json", true, false)
	.Build();

var cs = configuration.GetConnectionString("azurebus");

var services = new ServiceCollection();

services.AddArianeBus(config =>
{
	config.BusConnectionString = cs!;
	//config.SendStrategyName = $"{SendStrategy.OneByOne}";
});

services.AddLogging(config =>
{
	config.SetMinimumLevel(LogLevel.Trace);
	config.AddConsole();
});

var sp = services.BuildServiceProvider();

var bus = sp.GetRequiredService<IServiceBus>();

Console.WriteLine("Start send messages");
var sw = new Stopwatch();
sw.Start();
var messageCount = 451;
for (int i = 0; i < messageCount; i++)
{
	var request = new ConsoleWriter.SampleMessageRequest() { RowNumber = i };
	await bus.EnqueueMessage("test1", request);
}

sw.Stop();
Console.WriteLine($"{messageCount} send in {sw.ElapsedMilliseconds}ms");

Console.Read();
