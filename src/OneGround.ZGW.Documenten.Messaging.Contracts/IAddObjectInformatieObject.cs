using System;
using OneGround.ZGW.Common.Messaging;

namespace OneGround.ZGW.Documenten.Messaging.Contracts;

public interface IAddObjectInformatieObject : IRsinContract
{
    string InformatieObject { get; set; }
    string Object { get; set; }
    string ObjectType { get; set; }
    Guid CorrelationId { get; set; }
}
