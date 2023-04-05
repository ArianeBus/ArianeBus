using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace ArianeBus.Tests;

internal class OrderedPersonReader : ArianeBus.MessageReaderBase<Person>
{
	private readonly MessageCollector _messageCollector;
	private readonly ILogger _logger;
	private int _id = -1;

	public OrderedPersonReader(MessageCollector messageCollector,
		ILogger<PersonReader> logger)
	{
		_messageCollector = messageCollector;
		_logger = logger;
	}

	public override Task ProcessMessageAsync(Person message, CancellationToken cancellationToken)
	{
		if (_id == -1)
		{
			_id = message.Id;
		}
		else
		{
			if (_id + 1 != message.Id)
			{
				throw new Exception("Not ordered");
			}
			_id = message.Id;
		}

		message.IsProcessed = true;

		_messageCollector.AddPerson(message);
		_logger.LogInformation("{threadName}:{firstName}", System.Threading.Thread.CurrentThread.Name, message.FirstName);
		return Task.CompletedTask;
	}
}
