using System;
using MassTransit;

namespace OneGround.ZGW.Common.Messaging;

public interface IDocumentPrioritizedJob : CorrelatedBy<Guid>, IRsinContract
{
    Guid EnkelvoudigInformatieObjectId { get; set; }
}
