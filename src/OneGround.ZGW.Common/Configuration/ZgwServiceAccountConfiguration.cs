using System.Collections.Generic;

namespace OneGround.ZGW.Common.Configuration;

public class ZgwServiceAccountConfiguration
{
    public required List<ZgwServiceAccountCredential> Credentials { get; set; }
}

public class ZgwServiceAccountCredential
{
    public required string Rsin { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
}
