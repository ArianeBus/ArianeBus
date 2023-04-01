namespace ArianeBus;

internal class QueueReaderRegistration
{
    public string QueueName { get; set; } = null!;
	internal Type ReaderType { get; set; } = default!;
}
