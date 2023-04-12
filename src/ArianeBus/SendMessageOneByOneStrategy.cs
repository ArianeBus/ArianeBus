using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure.Messaging.ServiceBus;

namespace ArianeBus;

internal class SendMessageOneByOneStrategy  : SendMessageStrategyBase
{
	public override string StrategyName => $"{SendStrategy.OneByOne}";

	public override async Task TrySendRequest(ServiceBusSender sender, MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		_messageAddedCount++;
		var messageBus = CreateServiceBusMessage(messageRequest);
		_messageProcessedCount++;
		await sender.SendMessageAsync(messageBus, cancellationToken);
		_messageSentCount++;
	}

}
