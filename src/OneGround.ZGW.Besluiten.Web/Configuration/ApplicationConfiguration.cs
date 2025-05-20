using OneGround.ZGW.Common.Web.Configuration;

namespace OneGround.ZGW.Besluiten.Web.Configuration;

public class ApplicationConfiguration
{
    public bool DontSendNotificaties { get; set; }
    public int BesluitenPageSize { get; set; }
    public bool IgnoreZaakValidation { get; set; }
    public bool IgnoreBesluitTypeValidation { get; set; }
    public bool IgnoreInformatieObjectValidation { get; set; }
    public int DrcSynchronizationTimeoutSeconds { get; set; } = 8;
    public bool DrcSynchronizationAsyncOnlyMode { get; set; } = false;
    public ExpandSettings ExpandSettings { get; set; } = new();
}
