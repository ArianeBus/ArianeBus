﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

namespace ArianeBus.Tests;

internal class PersonReader : ArianeBus.MessageReaderBase<Person>
{
	private readonly MessageCollector _messageCollector;
	private readonly ILogger _logger;

	public PersonReader(MessageCollector messageCollector,
		ILogger<PersonReader> logger)
    {
		_messageCollector = messageCollector;
		_logger = logger;
	}

	public override Task ProcessMessageAsync(Person message, CancellationToken cancellationToken)
	{
		message.IsProcessed = true;

		_messageCollector.AddPerson(message);
		_logger.LogInformation("Thread:{threadName} Person:{firstName} message count {count}", Environment.CurrentManagedThreadId, message.FirstName, _messageCollector.Count);
		return Task.CompletedTask;
	}
}
