using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus;

internal interface IQueueReader
{
	string QueueOrTopicName { get; set; }
	Type MessageType { get; set; }
	Type ReaderType { get; set; }
}
