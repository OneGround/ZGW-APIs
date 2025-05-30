using System;
using System.Collections.Generic;
using MassTransit;

namespace Roxit.ZGW.Common.Messaging;

public interface INotificatie : CorrelatedBy<Guid>, IRsinContract
{
    string Kanaal { get; }
    string HoofdObject { get; }
    string Resource { get; }
    string ResourceUrl { get; }
    string Actie { get; }
    IDictionary<string, string> Kenmerken { get; }
}

public interface ISendNotificaties : INotificatie
{
    bool Ignore { get; }
}
