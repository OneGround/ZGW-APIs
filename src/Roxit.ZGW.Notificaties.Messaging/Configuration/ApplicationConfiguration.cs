namespace Roxit.ZGW.Notificaties.Messaging.Configuration;

public class ApplicationConfiguration
{
    public TimeSpan CallbackTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan AbonnementenCacheExpirationTime { get; set; } = TimeSpan.FromMinutes(2);
}
