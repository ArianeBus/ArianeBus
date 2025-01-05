namespace ArianeBus;

public class QueueOrTopicBehaviorOptions
{
	public string SendStrategyName { get; set; } = $"{SendStrategy.Bufferized}";
}
