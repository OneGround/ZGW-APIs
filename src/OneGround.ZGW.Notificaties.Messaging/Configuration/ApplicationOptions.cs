namespace OneGround.ZGW.Notificaties.Messaging.Configuration;

public class ApplicationOptions
{
    public const string Application = "Application";

    public TimeSpan CallbackTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan AbonnementenCacheExpirationTime { get; set; } = TimeSpan.FromMinutes(2);
}
