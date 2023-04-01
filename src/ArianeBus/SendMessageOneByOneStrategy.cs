using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure.Messaging.ServiceBus;

namespace ArianeBus;

internal class SendMessageOneByOneStrategy  : SendMessageStrategyBase
{
	private readonly ServiceBuSenderFactory _serviceBuSenderFactory;

	public SendMessageOneByOneStrategy(ServiceBuSenderFactory serviceBuSenderFactory)
    {
		_serviceBuSenderFactory = serviceBuSenderFactory;
	}

	public override string StrategyName => $"{SendStrategy.OneByOne}";

	internal override async Task TrySendRequest(MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		_messageAddedCount++;
		var sender = await _serviceBuSenderFactory.GetSender(messageRequest, cancellationToken);
		var messageBus = CreateServiceBusMessage(messageRequest);
		_messageProcessedCount++;
		await sender.SendMessageAsync(messageBus, cancellationToken);
		_messageSentCount++;
	}

}
