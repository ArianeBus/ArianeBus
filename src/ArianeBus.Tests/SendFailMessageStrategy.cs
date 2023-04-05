using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus.Tests;

public class SendFailMessageStrategy : SendMessageStrategyBase
{
	public override string StrategyName => "FailStrategy";

	public override Task TrySendRequest(MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		throw new HostAbortedException("Intentional fail");
	}
}
