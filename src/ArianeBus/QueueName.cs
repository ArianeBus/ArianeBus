namespace ArianeBus;

public record QueueName
{
	public QueueName(string value)
	{
		this.Value = value;
	}

	public string Value { get; init; }

	public override string ToString()
	{
		return $"{Value}";
	}
}
