using System;
using Roxit.ZGW.Common.Contracts.v1;

namespace Roxit.ZGW.Common.ServiceAgent;

public class ServiceAgentResponse
{
    public ErrorResponse Error { get; }
    public Exception Exception { get; }
    public bool Success => Error == null && Exception == null;

    public ServiceAgentResponse() { }

    public ServiceAgentResponse(ErrorResponse errorResponse, Exception exception = null)
    {
        Error = errorResponse;
        Exception = exception;
    }
}

public class ServiceAgentResponse<TResponse> : ServiceAgentResponse
{
    public TResponse Response { get; }

    public ServiceAgentResponse(TResponse response)
    {
        Response = response;
    }

    public ServiceAgentResponse(ErrorResponse response, Exception exception = null)
        : base(response, exception) { }
}
