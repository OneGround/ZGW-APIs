using System;
using OneGround.ZGW.Common.Messaging;

namespace OneGround.ZGW.Documenten.Messaging.Contracts;

public interface IDestroyEnkelvoudigInformatieObject : IRsinContract
{
    string EnkelvoudigInformatieObjectUrl { get; set; }
    string ObjectUrl { get; set; }
    Guid CorrelationId { get; set; }
}
