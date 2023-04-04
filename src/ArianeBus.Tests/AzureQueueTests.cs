using ArianeBus.Tests;

namespace ArianeBus.Tests;

[TestClass]
public class AzureQueueTests
{
	private static IHost? _host;

	[ClassInitialize()]
	public static void Initialize(TestContext testContext)
	{
		_host = RootTest.CreateHost(config =>
		{
			config.RegisterQueueReader<PersonReader>(new QueueName("test.azure2"));
			config.RegisterQueueReader<PersonReader>(new QueueName("test.onebyone"));
			config.RegisterQueueOrTopicBehaviorOptions("test.onebyone", options =>
			{
				options.SendStrategyName = $"{SendStrategy.OneByOne}";
			});
		});
	}

	[TestMethod]
	public async Task Send_And_Receive_Person_Message()
	{
		var bus = _host!.Services.GetRequiredService<IServiceBus>();

		var messageCollector = _host.Services.GetRequiredService<MessageCollector>();
		messageCollector.Reset();

		var person = new Person();
		person.FirstName = Guid.NewGuid().ToString();
		person.LastName = Guid.NewGuid().ToString();

		await bus.EnqueueMessage("test.azure2", person);

		await _host.StartAsync();

		await messageCollector.WaitForReceiveMessage(5 * 1000);
		messageCollector.Count.Should().BeGreaterThan(0);

		await _host.StopAsync();
	}

	[TestMethod]
	public async Task Send_And_Receive_Person_Message_One_By_One()
	{
		var bus = _host!.Services.GetRequiredService<IServiceBus>();

		var messageCollector = _host.Services.GetRequiredService<MessageCollector>();
		messageCollector.Reset();

		for (int i = 0; i < 10; i++)
		{
			var person = new Person();
			person.FirstName = Guid.NewGuid().ToString();
			person.LastName = Guid.NewGuid().ToString();

			await bus.EnqueueMessage("test.onebyone", person);
			await bus.EnqueueMessage("test.onebyone", person);
			await bus.EnqueueMessage("test.onebyone", person);
			await bus.EnqueueMessage("test.onebyone", person);
		}

		await _host.StartAsync();

		await messageCollector.WaitForReceiveMessage(5 * 1000);
		messageCollector.Count.Should().BeGreaterThan(0);

		await _host.StopAsync();
	}


	[TestMethod]
	public async Task Send_And_Receive_Person_Message_With_Options()
	{
		var bus = _host!.Services.GetRequiredService<IServiceBus>();

		var messageCollector = _host.Services.GetRequiredService<MessageCollector>();
		messageCollector.Reset();

		var person = new Person();
		person.FirstName = Guid.NewGuid().ToString();
		person.LastName = Guid.NewGuid().ToString();

		await bus.EnqueueMessage("test.azure2", person, new MessageOptions
		{
			Subject = "test messag",
			ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddSeconds(20),
		});

		await _host.StartAsync();

		await messageCollector.WaitForReceiveMessage(1 * 1000);
		messageCollector.Count.Should().Be(0);

		await messageCollector.WaitForReceiveMessage(25 * 1000);
		messageCollector.Count.Should().BeGreaterThan(0);

		await _host.StopAsync();
	}


	[TestMethod]
	public async Task Write_In_Queue_And_Receive()
	{
		var bus = _host!.Services.GetRequiredService<IServiceBus>();

		var person = Person.CreateTestPerson();
		await bus.EnqueueMessage("test.azure3", person);

		await Task.Delay(5 * 1000);

		var list = await bus.ReceiveAsync<Person>(new QueueName("test.azure3"), 10, 5 * 1000);

		list.Should().NotBeNullOrEmpty();
	}

	[TestMethod]
	public async Task Clear_Queue()
	{
		var bus = _host!.Services.GetRequiredService<IServiceBus>();

		var person = Person.CreateTestPerson();
		await bus.EnqueueMessage("test.clear", person);

		await Task.Delay(5 * 1000);

		await bus.ClearQueue(new QueueName("test.clear"));

		var list = await bus.ReceiveAsync<Person>(new QueueName("test.clear"), 10, 5 * 1000);

		list.Should().BeNullOrEmpty();
	}

	[TestMethod]
	public async Task Create_And_Delete_Queue()
	{
		var bus = _host!.Services.GetRequiredService<IServiceBus>();

		var queueName = new QueueName($"{Guid.NewGuid()}");
		await bus.CreateQueue(queueName);

		var exists = await bus.IsQueueExists(queueName);
		exists.Should().BeTrue();

		await Task.Delay(5 * 1000);

		await bus.DeleteQueue(queueName);
		exists = await bus.IsQueueExists(queueName);
		exists.Should().BeFalse();

	}
}
