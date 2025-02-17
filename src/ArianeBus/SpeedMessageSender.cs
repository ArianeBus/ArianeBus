using System.Net.Http.Json;

namespace ArianeBus;

internal class SpeedMessageSender(
	IHttpClientFactory httpClientFactory,
	ArianeSettings settings,
	ILogger<SpeedMessageSender> logger)
{
	public async Task EnqueueMessage(string queueName, object message, CancellationToken cancellationToken)
	{
		var cs = new AzureBusConnectionString(settings.BusConnectionString);
		string url = $"https://{cs.Namespace}.servicebus.windows.net/{queueName}/messages".ToLower();

		var client = httpClientFactory.CreateClient("AzureBus");
		var response = await client.PostAsJsonAsync(url, message, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			var detail = await response.Content.ReadAsStringAsync(cancellationToken);
			logger.LogError("Erreur lors de l'envoi du message. {Statut} {Detail}", response.StatusCode, detail);
		}
	}

	public async Task PublishTopic(string topicName, object message, CancellationToken cancellationToken)
	{
		var cs = new AzureBusConnectionString(settings.BusConnectionString);
		string url = $"https://{cs.Namespace}.servicebus.windows.net/{topicName}/messages".ToLower();
		var client = httpClientFactory.CreateClient("AzureBus");
		var response = await client.PostAsJsonAsync(url, message, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			var detail = await response.Content.ReadAsStringAsync(cancellationToken);
			logger.LogError("Erreur lors de l'envoi du message. {Statut} {Detail}", response.StatusCode, detail);
		}
	}
}
