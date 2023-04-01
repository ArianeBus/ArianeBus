using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

using Azure.Core;

using Microsoft.Extensions.Azure;

namespace ArianeBus;

internal class ServiceBuSenderFactory
{
	private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();
	private readonly ServiceBusClient _serviceBusClient;
	private readonly ArianeSettings _settings;
	private readonly ILogger _logger;

	public ServiceBuSenderFactory(ServiceBusClient serviceBusClient,
		ArianeSettings settings,
		ILogger<ServiceBuSenderFactory> logger)
	{
		_serviceBusClient = serviceBusClient;
		_settings = settings;
		_logger = logger;
	}

	public async Task<ServiceBusSender> GetSender(MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		if (_senders.TryGetValue(messageRequest.QueueOrTopicName, out var sender))
		{
			return sender;
		}

		if (messageRequest.QueueType == QueueType.Queue)
		{
			await _settings.CreateQueueIfNotExists(messageRequest.QueueOrTopicName
				, _logger,
				cancellationToken);
		}

		sender = _serviceBusClient.CreateSender(messageRequest.QueueOrTopicName);
		_senders.TryAdd(messageRequest.QueueOrTopicName, sender);
		return sender;
	}
}
