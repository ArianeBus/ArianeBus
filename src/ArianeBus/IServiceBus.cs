using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus;

public interface IServiceBus
{
	Task<IEnumerable<T>> ReceiveAsync<T>(string queueName, int count, int timeoutInMillisecond);
	Task PublishTopic<T>(string topicName, T request, MessageOptions? options = null, CancellationToken? cancellationToken = null)
		where T : class, new();
	Task EnqueueMessage<T>(string queueName, T request, MessageOptions? options = null, CancellationToken? cancellationToken = null)
		where T : class, new();

}
