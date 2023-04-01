using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ConsoleWriter;

namespace ConsoleReader;

internal class SampleMessageReader : ArianeBus.MessageReaderBase<ConsoleWriter.SampleMessageRequest>
{
	private readonly ILogger<SampleMessageReader> _logger;

	public SampleMessageReader(ILogger<SampleMessageReader> logger)
    {
		_logger = logger;
	}
    public override Task ProcessMessageAsync(SampleMessageRequest message, CancellationToken cancellationToken)
	{
		_logger.LogInformation("{message}", message);
		return Task.CompletedTask;
	}
}
