# ArianeBus

Send and Receive messages massively with Azure Bus 

## Where can I get it ?

**First**, install [ArianeBus](https://www.nuget.org/packages/AzureBus) from the package manager console in your app.

> PM> Install-Package ArianeBus

Create Message

```csharp
public class SampleMessage
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public int RowNumber { get; set; }

    public override string ToString()
    {
        return $"{MessageId}.{RowNumber}";
    }
}
```

Next configure ArianeBus Writer in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("AzureBus");

builder.Services.AddArianeBus(config =>
{
    config.BusConnectionString = cs!;
});

var app = builder.Build();

var bus = sp.GetRequiredService<IServiceBus>();

var message = new ConsoleWriter.SampleMessage() { RowNumber = 1 };
await bus.EnqueueMessage("test1", message);

```

ArianeBus create queue if not exists and send the request in Azure Bus

Next Create MessageReader

```csharp
internal class SampleMessageReader : MessageReaderBase<SampleMessage>
{
	private readonly ILogger _logger;

	public SampleMessageRequestHandler(ILogger<SampleMessageRequestHandler> logger)
	{
		_logger = logger;
	}

	public Task ProcessMessageAsync(SampleMessage message, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Message : {message}", message);
		return Task.CompletedTask;
	}
}
```

Configure ArianeBus for Reader in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("azurebus");

builder.Services.AddArianeBus(config =>
{
	config.BusConnectionString = cs!;
	config.RegisterQueueReader<SampleMessageReader>(new QueueName("test1"));
});

var app = builder.Build();

app.Run();
```