using System.Net;

namespace Roxit.ZGW.Notificaties.Messaging;

public class SubscriberResult
{
    public bool Success { get; set; }
    public HttpStatusCode? StatusCode { get; set; }
    public string Message { get; set; }
    public bool? Timeout { get; set; }
}
