using System.Collections.Generic;
using OneGround.ZGW.Common.Web.Configuration;

namespace OneGround.ZGW.Zaken.Web.Configuration;

public class ApplicationConfiguration
{
    public bool DontSendNotificaties { get; set; }
    public int ZakenPageSize { get; set; }
    public int ZaakStatussenPageSize { get; set; }
    public int ZaakObjectenPageSize { get; set; }
    public int ZaakInformatieObjectenPageSize { get; set; }
    public int ZaakResultatenPageSize { get; set; }
    public int ZaakRollenPageSize { get; set; }
    public bool IgnoreZaakTypeValidation { get; set; }
    public bool IgnoreStatusTypeValidation { get; set; }
    public bool IgnoreInformatieObjectValidation { get; set; }
    public int KlantContactenPageSize { get; set; }
    public bool SkipMigrationsAtStartup { get; set; }
    public bool ApplyFixturesAtStartup { get; set; }
    public string FixturesSource { get; set; }
    public int DrcSynchronizationTimeoutSeconds { get; set; } = 8;
    public bool DrcSynchronizationAsyncOnlyMode { get; set; } = false;
    public Dictionary<string, string> NummerGeneratorFormats { get; set; } = [];
    public ExpandSettings ExpandSettings { get; set; } = new();
}
