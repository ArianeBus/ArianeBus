using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArianeBus;

public abstract class MessageReaderBase<T> : IMessageReader
{
	/// <summary>
	/// Name of queue 
	/// </summary>
	public virtual string QueueOrTopicName { get; set; } = null!;
	/// <summary>
	/// Topic name for multiple subscription
	/// </summary>
	public virtual string? FromSubscriptionName { get; set; }
	/// <summary>
	/// Starts processing the associated message
	/// </summary>
	/// <param name="message"></param>
	public abstract Task ProcessMessageAsync(T message, CancellationToken cancellationToken);
}
