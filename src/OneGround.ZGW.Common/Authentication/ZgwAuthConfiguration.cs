namespace OneGround.ZGW.Common.Authentication;

public record ZgwAuthConfiguration
{
    public string Authority { get; init; }
    public string ValidAudience { get; init; }
}
