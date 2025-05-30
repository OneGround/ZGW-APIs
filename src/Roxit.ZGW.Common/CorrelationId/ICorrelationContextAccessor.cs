using System;

namespace Roxit.ZGW.Common.CorrelationId;

public interface ICorrelationContextAccessor
{
    string CorrelationId { get; }
    IDisposable SetCorrelationId(string correlationId);
}
