namespace ArianeBus;
internal class AzureBusConnectionString
{
	public AzureBusConnectionString(string connectionString)
	{
		var parts = connectionString.Split(';');
		foreach (var part in parts)
		{
			var key = part.Split('=')[0];
			switch (key)
			{
				case "Endpoint":
					Endpoint = part.Split('=')[1];
					Namespace = Endpoint.Replace("sb://", "").Split('.')[0];
					break;
				case "SharedAccessKeyName":
					SharedAccessKeyName = part.Split('=')[1];
					break;
				case "SharedAccessKey":
					SharedAccessKey = part.Replace("SharedAccessKey=", "");
					break;
			}
		}

	}

	public string Endpoint { get; set; } = null!;
	public string Namespace { get; set; } = null!;
	public string SharedAccessKeyName { get; set; } = null!;
	public string SharedAccessKey { get; set; } = null!;
}
