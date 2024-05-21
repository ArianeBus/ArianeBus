using MediatR;

using Microsoft.Extensions.Logging;

namespace ArianeBus.MediatR;

internal record GetLoggerRequest : IRequest<ILogger<GetLoggerRequestHandler>>;

internal class GetLoggerRequestHandler(
	ILogger<GetLoggerRequestHandler> logger
	)
	: IRequestHandler<GetLoggerRequest, ILogger<GetLoggerRequestHandler>>
{
	public Task<ILogger<GetLoggerRequestHandler>> Handle(GetLoggerRequest request, CancellationToken cancellationToken)
	{
		return Task.FromResult(logger);
	}
}
