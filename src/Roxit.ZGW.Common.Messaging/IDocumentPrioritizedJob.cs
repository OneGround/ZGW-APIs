using System;
using MassTransit;

namespace Roxit.ZGW.Common.Messaging;

public interface IDocumentPrioritizedJob : CorrelatedBy<Guid>, IRsinContract
{
    Guid EnkelvoudigInformatieObjectId { get; set; }
}
