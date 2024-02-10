using MediatR;

namespace ArianeBus.MediatR;

internal record ArianeBusConfig(IServiceBus Bus, MediatRBusConfiguration Configuration);
internal record GetBusRequest : IRequest<ArianeBusConfig>;

internal class GetBusConfigRequestHandler(
	IServiceBus serviceBus,
	MediatRBusConfiguration configuration
	) : IRequestHandler<GetBusRequest, ArianeBusConfig>
{
	public Task<ArianeBusConfig> Handle(GetBusRequest request, CancellationToken cancellationToken)
	{
		var result = new ArianeBusConfig(serviceBus, configuration);
		return Task.FromResult(result);
	}
}
