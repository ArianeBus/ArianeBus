using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus;

internal interface ITopicReader
{
	string TopicName { get; set; }
	string SubscriptionName { get; set; }
	Type MessageType { get; set; }
	Type ReaderType { get; set; }
}
