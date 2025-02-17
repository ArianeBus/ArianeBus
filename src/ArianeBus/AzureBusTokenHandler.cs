using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;

using Microsoft.Extensions.Caching.Memory;

namespace ArianeBus;
internal class AzureBusTokenHandler(
	ArianeSettings settings,
	IMemoryCache cache
	)
	: DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var requestUri = $"{request.RequestUri}";
		cache.TryGetValue(requestUri, out string? sasToken);
		if (sasToken is null)
		{
			var cs = new AzureBusConnectionString(settings.BusConnectionString);

			// Définir l'heure d'expiration du jeton en secondes depuis l'époque Unix
			var sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);
			string expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + 3600);

			// Créer la chaîne à signer
			string stringToSign = WebUtility.UrlEncode(requestUri) + "\n" + expiry;

			// Signer la chaîne avec la clé d'accès partagé en utilisant HMAC-SHA256
			using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(cs.SharedAccessKey));
			string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

			// Construire le jeton SAS
			sasToken = $"sr={WebUtility.UrlEncode(requestUri)}&sig={WebUtility.UrlEncode(signature)}&se={expiry}&skn={cs.SharedAccessKeyName}";
			cache.Set(requestUri, sasToken, DateTime.Now.AddSeconds(3500));
		}

		request.Headers.Authorization = new AuthenticationHeaderValue("SharedAccessSignature", sasToken);
		return await base.SendAsync(request, cancellationToken);
	}
}
