using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ArianeBus;

internal class MessageBuffer
{
	private readonly System.Timers.Timer _timeout = new(TimeSpan.FromSeconds(1));

	public MessageBuffer()
    {
		_timeout.Elapsed += TimerElapsed;
		_timeout.Start();
	}

	public ServiceBusMessageBatch Batch { get; set; } = default!;

	public Func<ServiceBusMessageBatch, Task> OnTimeout = default!;
	public bool IsProcessed { get; set; } = false;

    private async void TimerElapsed(object? source, ElapsedEventArgs e)
	{
		if (OnTimeout is not null
			&& !IsProcessed)
		{
			await OnTimeout(Batch);
		}
		_timeout.Stop();
		_timeout.Elapsed -= TimerElapsed;
	}
}
