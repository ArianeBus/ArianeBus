using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ArianeBus;

internal sealed class MessageBuffer : IDisposable
{
	private readonly System.Timers.Timer _timeout = new(TimeSpan.FromSeconds(2));

	public MessageBuffer()
    {
		_timeout.Elapsed += TimerElapsed;
		_timeout.Start();
	}

	public ServiceBusMessageBatch Batch { get; set; } = default!;

	public Action<ServiceBusMessageBatch, MessageBuffer> OnTimeout = default!;
	public bool IsProcessed { get; set; } = false;

    private void TimerElapsed(object? source, ElapsedEventArgs e)
	{
		if (OnTimeout is not null
			&& !IsProcessed)
		{
			OnTimeout(Batch, this);
		}
		Dispose();
	}

	public void Dispose()
	{
		_timeout.Stop();
		_timeout.Elapsed -= TimerElapsed;
	}
}
