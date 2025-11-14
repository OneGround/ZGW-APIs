using System;

namespace OneGround.ZGW.Common.Secrets;

public class ServiceSecret
{
    public string Rsin { get; set; }
    public string Name { get; set; }
    public string ClientId { get; set; }
    public string Secret { get; set; }
    public string Api { get; set; }
    public string ProductName { get; set; }
    // public TimeSpan ExpiresIn { get; set; } // TODO Extend this class with expiration handling
}
