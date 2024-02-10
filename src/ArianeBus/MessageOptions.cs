namespace ArianeBus;

public class MessageOptions
{
	public string? Subject { get; set; }
	public TimeSpan? TimeToLive { get; set; }
	public DateTime? ScheduledEnqueueTimeUtc { get; set; }
}
