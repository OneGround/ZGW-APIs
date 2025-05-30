using System;
using Roxit.ZGW.Common.Messaging;

namespace Roxit.ZGW.Documenten.Messaging.Contracts;

public interface IAddObjectInformatieObject : IRsinContract
{
    string InformatieObject { get; set; }
    string Object { get; set; }
    string ObjectType { get; set; }
    Guid CorrelationId { get; set; }
}
