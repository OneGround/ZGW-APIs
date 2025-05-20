using System.Collections.Generic;

namespace OneGround.ZGW.Common.Configuration;

public class ZgwServiceAccountConfiguration
{
    public Dictionary<string, ZgwServiceAccountCredential> ZgwServiceAccountCredentials { get; set; }
}

public class ZgwServiceAccountCredential
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
}
