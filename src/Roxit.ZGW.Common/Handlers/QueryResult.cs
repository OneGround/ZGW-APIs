using System.Collections.Generic;
using Roxit.ZGW.Common.Contracts.v1;

namespace Roxit.ZGW.Common.Handlers;

public class QueryResult<TResult>
{
    public QueryResult(TResult result, QueryStatus status, params ValidationError[] errors)
    {
        Result = result;
        Status = status;
        Errors = errors;
    }

    public TResult Result { get; }
    public QueryStatus Status { get; }
    public IList<ValidationError> Errors { get; }
}
