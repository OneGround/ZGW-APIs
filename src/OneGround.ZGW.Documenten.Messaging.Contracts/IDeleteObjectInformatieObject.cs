using System;
using OneGround.ZGW.Common.Messaging;

namespace OneGround.ZGW.Documenten.Messaging.Contracts;

public interface IDeleteObjectInformatieObject : IRsinContract
{
    string InformatieObject { get; set; }
    string Object { get; set; }
    bool ObjectDestroy { get; set; }
    Guid CorrelationId { get; set; }
}
