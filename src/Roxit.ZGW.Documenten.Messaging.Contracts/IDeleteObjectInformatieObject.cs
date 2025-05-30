using System;
using Roxit.ZGW.Common.Messaging;

namespace Roxit.ZGW.Documenten.Messaging.Contracts;

public interface IDeleteObjectInformatieObject : IRsinContract
{
    string InformatieObject { get; set; }
    string Object { get; set; }
    bool ObjectDestroy { get; set; }
    Guid CorrelationId { get; set; }
}
