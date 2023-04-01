using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus;

internal class SendAction
{
	public ServiceBusSender Sender { get; set; } = default!;
	public Action Action { get; set; } = default!;
}
