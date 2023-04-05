using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus.Tests;

[TestClass]
public class AzureTopicTests
{
	[TestMethod]
	public async Task Send_And_Receive_Person_Queue()
	{
		var topic = new TopicName($"topic.{Guid.NewGuid()}");
		var host = RootTest.CreateHost(config =>
		{
			config.RegisterTopicReader<PersonReader>(topic, new SubscriptionName("sub1"));
			config.RegisterTopicReader<PersonReader>(topic, new SubscriptionName("sub2"));
			config.RegisterTopicReader<PersonReader>(topic, new SubscriptionName("sub3"));
		});

		var bus = host!.Services.GetRequiredService<IServiceBus>();

		await host.StartAsync();

		var messageCollector = host.Services.GetRequiredService<MessageCollector>();
		messageCollector.Reset(3);

		var person = new Person();
		person.FirstName = Guid.NewGuid().ToString();
		person.LastName = Guid.NewGuid().ToString();

		await bus.PublishTopic(topic.Value, person);

		await Task.Delay(10 * 1000);

		await messageCollector.WaitForReceiveMessage(10 * 1000);

		messageCollector.Count.Should().Be(3);

		await host.StopAsync();

		await bus.DeleteTopic(topic);
	}

	[TestMethod]
	public void Register_Same_Topic_And_Subscription()
	{
		var host = RootTest.CreateHost(config =>
		{
			config.RegisterTopicReader<PersonReader>(new TopicName("MyTopic"), new SubscriptionName("sub1"));
			config.RegisterTopicReader<PersonReader>(new TopicName("MyTopic"), new SubscriptionName("sub1"));
		});

		var bus = host!.Services.GetRequiredService<IServiceBus>();
		var topicList = bus.GetRegisteredTopicAndSubscriptionNameList();

		topicList.Count.Should().Be(1);
	}


	[TestMethod]
	public async Task Clear_Topic()
	{
		var host = RootTest.CreateHost(config =>
		{
			config.RegisterTopicReader<PersonReader>(new TopicName("topic.clear"), new SubscriptionName("subclear"));
		});

		var bus = host!.Services.GetRequiredService<IServiceBus>();

		await bus.CreateTopicAndSubscription(new TopicName("topic.clear"), new SubscriptionName("subclear"));

		var person = Person.CreateTestPerson();
		await bus.PublishTopic("topic.clear", person);

		await bus.ClearTopic(new TopicName("topic.clear"), new SubscriptionName("subclear"));

		var list = await bus.ReceiveAsync<Person>(new TopicName("topic.clear"), new SubscriptionName("subclear"), 10, 5 * 1000);

		list.Should().BeNullOrEmpty();
	}

	[TestMethod]
	public async Task Create_And_Delete_Topic_And_Subscription()
	{
		var host = RootTest.CreateHost();

		var bus = host!.Services.GetRequiredService<IServiceBus>();

		var topicName = new TopicName($"{Guid.NewGuid()}");
		var subscriptionName1 = new SubscriptionName($"{Guid.NewGuid()}");
		var subscriptionName2 = new SubscriptionName($"{Guid.NewGuid()}");
		await bus.CreateTopic(topicName);
		await bus.CreateTopicAndSubscription(topicName, subscriptionName1);
		await bus.CreateTopicAndSubscription(topicName, subscriptionName2);

		var exists = await bus.IsTopicExists(topicName);
		exists.Should().BeTrue();

		exists = await bus.IsSubscriptionExists(topicName, subscriptionName1);
		exists.Should().BeTrue();

		exists = await bus.IsSubscriptionExists(topicName, subscriptionName2);
		exists.Should().BeTrue();

		await bus.DeleteTopic(topicName);

		exists = await bus.IsTopicExists(topicName);
		exists.Should().BeFalse();

		exists = await bus.IsSubscriptionExists(topicName, subscriptionName1);
		exists.Should().BeFalse();

		await bus.CreateTopic(topicName);

		await bus.CreateTopicAndSubscription(topicName, subscriptionName1);

		exists = await bus.IsSubscriptionExists(topicName, subscriptionName1);
		exists.Should().BeTrue();

		await bus.DeleteSubscription(topicName, subscriptionName1);
		exists = await bus.IsSubscriptionExists(topicName, subscriptionName1);
		exists.Should().BeFalse();

		await bus.DeleteTopic(topicName);

		exists = await bus.IsTopicExists(topicName);
		exists.Should().BeFalse();
	}

}
