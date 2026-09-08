using System;
using OneGround.ZGW.Common.Handlers;

namespace OneGround.ZGW.Common.Web.Expands;

public sealed class ExpandInternalQueryHandlerException : Exception
{
    public ExpandInternalQueryHandlerException(string resource, QueryStatus statuscode)
        : base()
    {
        Resource = resource;
        StatusCode = statuscode;
    }

    public string Resource { get; private set; }
    public QueryStatus StatusCode { get; private set; }
}
