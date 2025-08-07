namespace OneGround.ZGW.Notificaties.ServiceAgent;

public sealed record NotificatieServiceConfiguration
{
    public const string Key = "NotificatieService";

    public NotificatieServiceType Type { get; init; }
}
