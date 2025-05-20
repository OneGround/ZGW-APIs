using System;

namespace OneGround.ZGW.Common.CorrelationId;

public interface ICorrelationContextAccessor
{
    string CorrelationId { get; }
    IDisposable SetCorrelationId(string correlationId);
}
