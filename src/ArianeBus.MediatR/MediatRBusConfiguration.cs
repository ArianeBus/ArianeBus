using System.Reflection;
using System.Text.Json;

namespace ArianeBus.MediatR;
public class MediatRBusConfiguration
{
	public string TopicName { get; set; } = null!;
	public string SubscriptionName { get; set; } = Assembly.GetEntryAssembly()?.GetName().Name!;
	public bool DiagnosticMessageEnabled { get; set; } = false;
}
