using System.Collections.Generic;
using OneGround.ZGW.Common.Web.Configuration;

namespace OneGround.ZGW.Zaken.Web.Configuration;

public class ApplicationConfiguration
{
    public bool DontSendNotificaties { get; set; }
    public int ZakenPageSize { get; set; } = 50;
    public int ZaakStatussenPageSize { get; set; } = 100;
    public int ZaakObjectenPageSize { get; set; } = 100;
    public int ZaakInformatieObjectenPageSize { get; set; } = 100;
    public int ZaakResultatenPageSize { get; set; } = 100;
    public int ZaakRollenPageSize { get; set; } = 100;
    public int KlantContactenPageSize { get; set; } = 100;
    public int ZaakNotitiesPageSize { get; set; } = 100;
    public bool IgnoreZaakTypeValidation { get; set; }
    public bool IgnoreStatusTypeValidation { get; set; }
    public bool IgnoreInformatieObjectValidation { get; set; }

    public bool SkipMigrationsAtStartup { get; set; }
    public bool ApplyFixturesAtStartup { get; set; }
    public string FixturesSource { get; set; }
    public int DrcSynchronizationTimeoutSeconds { get; set; } = 8;
    public bool DrcSynchronizationAsyncOnlyMode { get; set; } = false;
    public Dictionary<string, string> NummerGeneratorFormats { get; set; } = [];
    public int ReserveZaaknummersMaxAantal { get; set; } = 100;
    public ExpandSettings ExpandSettings { get; set; } = new();
}
