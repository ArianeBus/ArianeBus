using System.Reflection.Emit;

using ArianeBus.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArianeBus.Tests;

[TestClass]
public class AzureQueueTests
{
	[TestMethod]
	public async Task Send_And_Receive_Person_Message_With_Obsolete()
	{
		var queueName = new QueueName($"test.{Guid.NewGuid()}");
		var host = RootTest.CreateHost(config =>
		{
			config.RegisterQueueReader<PersonReader>(queueName);
		});

		var bus = host!.Services.GetRequiredService<IServiceBus>();

		await bus.CreateQueue(queueName);

		var messageCollector = host.Services.GetRequiredService<MessageCollector>();
		messageCollector.Reset(1);

		var person = Person.CreateTestPerson();
		await bus.SendAsync(queueName.Value, person);

		await host.StartAsync();

		await messageCollector.WaitForReceiveMessage(10 * 1000);
		messageCollector.Count.Should().BeGreaterThan(0);

		await host.StopAsync();

		await bus.DeleteQueue(queueName);
	}

	[TestMethod]
	public async Task Send_And_Receive_Person_Message()
	{
		var host = RootTest.CreateHost(config =>
		{
			config.RegisterQueueReader<PersonReader>(new QueueName("test.azure2"));
		});

		var bus = host!.Services.GetRequiredService<IServiceBus>();

		var messageCollector = host.Services.GetRequiredService<MessageCollector>();
		messageCollector.Reset();

		var person = new Person();
		person.FirstName = Guid.NewGuid().ToString();
		person.LastName = Guid.NewGuid().ToString();

		await bus.EnqueueMessage("test.azure2", person);

		await host.StartAsync();

		await messageCollector.WaitForReceiveMessage(6 * 1000);
		messageCollector.Count.Should().BeGreaterThan(0);

		await host.StopAsync();
	}

	[TestMethod]
	public void Register_Same_Queue()
	{
		var host = RootTest.CreateHost(config =>
		{
			config.RegisterQueueReader<PersonReader>(new QueueName("test.azure2"));
			config.RegisterQueueReader<PersonReader>(new QueueName("test.azure2"));
		});

		var bus = host!.Services.GetRequiredService<IServiceBus>();
		var registeredQueueList = bus.GetRegisteredQueueNameList();

		registeredQueueList.Count().Should().Be(1);
	}

	[TestMethod]
	public async Task Send_And_Receive_Person_Message_One_By_One()
	{
		var host = RootTest.CreateHost(config =>
		{
			config.RegisterQueueReader<PersonReader>(new QueueName("test.onebyone"));
			config.RegisterQueueOrTopicBehaviorOptions("test.onebyone", options =>
			{
				options.SendStrategyName = $"{SendStrategy.OneByOne}";
			});
		});

		var bus = host!.Services.GetRequiredService<IServiceBus>();

		var messageCollector = host.Services.GetRequiredService<MessageCollector>();
		messageCollector.Reset();

		for (int i = 0; i < 10; i++)
		{
			var person = new Person();
			person.FirstName = Guid.NewGuid().ToString();
			person.LastName = Guid.NewGuid().ToString();

			await bus.EnqueueMessage("test.onebyone", person);
		}

		await host.StartAsync();

		await messageCollector.WaitForReceiveMessage(5 * 1000);
		messageCollector.Count.Should().BeGreaterThan(0);

		await host.StopAsync();
	}


	[TestMethod]
	public async Task Send_And_Receive_Person_Message_With_Options()
	{
		var queueName = new QueueName($"test.schedule.{Guid.NewGuid()}");
		var host = RootTest.CreateHost(config =>
		{
			config.RegisterQueueReader<PersonReader>(queueName);
		});

		var bus = host!.Services.GetRequiredService<IServiceBus>();

		var messageCollector = host.Services.GetRequiredService<MessageCollector>();
		messageCollector.Reset();

		var person = Person.CreateTestPerson();
		await bus.EnqueueMessage(queueName.Value, person, new MessageOptions
		{
			Subject = "test message",
			TimeToLive = TimeSpan.FromMinutes(1),
			ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddSeconds(10),
		});

		await Task.Delay(5 * 1000);

		await host.StartAsync();

		await messageCollector.WaitForReceiveMessage(1 * 1000);
		messageCollector.Count.Should().Be(0);

		await messageCollector.WaitForReceiveMessage(60 * 1000);
		messageCollector.Count.Should().BeGreaterThan(0);

		await host.StopAsync();
		await bus.DeleteQueue(queueName);
	}


	[TestMethod]
	public async Task Write_In_Queue_And_Receive()
	{
		var host = RootTest.CreateHost();

		var bus = host!.Services.GetRequiredService<IServiceBus>();

		var person = Person.CreateTestPerson();
		var queueName = new QueueName($"test.{Guid.NewGuid()}");
		await bus.EnqueueMessage(queueName.Value, person);

		await Task.Delay(10 * 1000);

		var list = await bus.ReceiveAsync<Person>(queueName, 5, 10 * 1000);

		await bus.DeleteQueue(queueName);

		list.Should().NotBeNullOrEmpty();
	}

	[TestMethod]
	public async Task Clear_Queue()
	{
		var host = RootTest.CreateHost();

		var bus = host!.Services.GetRequiredService<IServiceBus>();

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
		var host = RootTest.CreateHost();

		var bus = host!.Services.GetRequiredService<IServiceBus>();

		var queueName = new QueueName($"{Guid.NewGuid()}");
		await bus.CreateQueue(queueName);

		var exists = await bus.IsQueueExists(queueName);
		exists.Should().BeTrue();

		await Task.Delay(5 * 1000);

		await bus.DeleteQueue(queueName);
		exists = await bus.IsQueueExists(queueName);
		exists.Should().BeFalse();

	}

	/// <summary>
	/// Test if the message is received by the reader in the same order as it was sent
	/// </summary>
	/// <returns></returns>
	[TestMethod]
	public async Task Stress_Test()
	{
		var queueName = new QueueName($"test.stress.{Guid.NewGuid()}");
		var hostReader = RootTest.CreateHost(config =>
		{
			config.RegisterQueueReader<OrderedPersonReader>(queueName);
		});

		var messageCollector = hostReader.Services.GetRequiredService<MessageCollector>();
		messageCollector.Reset(1000);
		await hostReader.StartAsync();

		var hostWriter = RootTest.CreateHost();

		var bus = hostWriter!.Services.GetRequiredService<IServiceBus>();
		for (int i = 0; i < 1000; i++)
		{
			var person = new Person
			{
				Id = i,
				FirstName = $"{i}",
				FromQueue = queueName.Value,
				LastName = $"{Guid.NewGuid()}"
			};
			await bus.EnqueueMessage(queueName.Value, person);
		}

		await messageCollector.WaitForReceiveMessage(60 * 1000);
		messageCollector.Count.Should().Be(1000);

		await hostReader.StopAsync();

		await bus.DeleteQueue(queueName);
	}

	[TestMethod]
	public async Task Send_Message_And_Fail()
	{
		var host = RootTest.CreateHost(config =>
		{
			config.RegisterQueueOrTopicBehaviorOptions("failqueue", options =>
			{
				options.SendStrategyName = "FailStrategy";
			});
		}, services =>
		{
			services.AddSingleton<SendMessageStrategyBase, SendFailMessageStrategy>();
		});

		var bus = host!.Services.GetRequiredService<IServiceBus>();

		var person = Person.CreateTestPerson();
		try
		{
			await bus.EnqueueMessage("failqueue", person);
		}
		catch (Exception ex)
		{
			ex.Should().BeOfType<HostAbortedException>();
		}

		await bus.DeleteQueue(new QueueName("failqueue"));
	}

	[TestMethod]
	public async Task Send_Message_With_Unknown_Sender()
	{
		var host = RootTest.CreateHost(config =>
		{
			config.RegisterQueueOrTopicBehaviorOptions("failqueue", options =>
			{
				options.SendStrategyName = "Unknown";
			});
		});

		var bus = host!.Services.GetRequiredService<IServiceBus>();

		var person = Person.CreateTestPerson();
		try
		{
			await bus.EnqueueMessage("failqueue", person);
		}
		catch (Exception ex)
		{
			ex.Should().BeOfType<ArgumentOutOfRangeException>();
		}

		await bus.DeleteQueue(new QueueName("failqueue"));
	}

	[TestMethod] 
	public async Task Send_With_Mock_Sender()
	{
		var host = RootTest.CreateHost(config =>
		{
			config.UseMockForUnitTests = true;
			config.RegisterQueueReader<PersonReader>(new QueueName("test.mock"));
		});

		var messageCollector = host.Services.GetRequiredService<MessageCollector>();
		messageCollector.Reset(1);

		var bus = host!.Services.GetRequiredService<IServiceBus>();
		var person = Person.CreateTestPerson();

		await bus.EnqueueMessage("test.mock", person);
		
		messageCollector.Count.Should().Be(1);
	}
}
