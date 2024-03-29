﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArianeBus
{
	public static class JsonSerializer
	{
		private static Lazy<System.Text.Json.JsonSerializerOptions> _lazyOptions = new Lazy<JsonSerializerOptions>(() =>
		{
			var options = new System.Text.Json.JsonSerializerOptions();
			options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
			options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
			options.PropertyNameCaseInsensitive = true;
			options.UnknownTypeHandling
				= System.Text.Json.Serialization.JsonUnknownTypeHandling.JsonNode;
			options.PropertyNamingPolicy = null;
			options.WriteIndented = false;
			options.ReadCommentHandling = JsonCommentHandling.Skip;
			return options;
		}, true);

		public static System.Text.Json.JsonSerializerOptions Options => _lazyOptions.Value;
	}
}
