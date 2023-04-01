
using ArianeBus.Tests;


namespace ArianeBus.Tests
{
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
			});
		}

		[TestMethod]
		public async Task Send_And_Receive_Person_Queue()
		{
			var mediator = _host!.Services.GetRequiredService<IServiceBus>();

			var messageCollector = _host.Services.GetRequiredService<MessageCollector>();
			messageCollector.Reset();

			var person = new Person();
			person.FirstName = Guid.NewGuid().ToString();
			person.LastName = Guid.NewGuid().ToString();

			await mediator.EnqueueMessage("test.azure2", person);

			await _host.StartAsync();

			await messageCollector.WaitForReceiveMessage(5 * 1000);
			messageCollector.Count.Should().BeGreaterThan(0);

			await _host.StopAsync();
		}

	}
}
