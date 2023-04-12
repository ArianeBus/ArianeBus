using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure.Messaging.ServiceBus;

namespace ArianeBus.Tests;

public class SendFailMessageStrategy : SendMessageStrategyBase
{
	public override string StrategyName => "FailStrategy";

	public override Task TrySendRequest(ServiceBusSender sender, MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		throw new HostAbortedException("Intentional fail");
	}
}
