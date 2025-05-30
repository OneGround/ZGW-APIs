using System.Collections.Generic;
using Roxit.ZGW.Common.Contracts.v1;

namespace Roxit.ZGW.Common.Handlers;

public class CommandResult
{
    public CommandResult(CommandStatus status, params ValidationError[] errors)
    {
        Status = status;
        Errors = errors;
    }

    public CommandResult(CommandStatus status, ErrorResponse errorResponse)
    {
        Status = status;
        ErrorResponse = errorResponse;
    }

    public CommandStatus Status { get; }
    public IList<ValidationError> Errors { get; }
    public ErrorResponse ErrorResponse { get; }
}

public class CommandResult<TResult> : CommandResult
{
    public CommandResult(TResult result, CommandStatus status, params ValidationError[] errors)
        : base(status, errors)
    {
        Result = result;
    }

    public TResult Result { get; }
}
