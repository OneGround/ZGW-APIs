using OneGround.ZGW.Common.Messaging;

namespace OneGround.ZGW.Notificaties.Messaging;

public class SubscriberNotificatie : INotificatie
{
    public string Rsin { get; set; }
    public Guid CorrelationId { get; set; }

    public string Kanaal { get; set; }
    public string HoofdObject { get; set; }
    public string Resource { get; set; }
    public string ResourceUrl { get; set; }
    public string Actie { get; set; }
    public IDictionary<string, string> Kenmerken { get; set; }
}
