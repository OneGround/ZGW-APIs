using Roxit.ZGW.Common.Web.Configuration;

namespace Roxit.ZGW.Documenten.Web.Configuration;

public class ApplicationConfiguration
{
    public bool DontSendNotificaties { get; set; }
    public int EnkelvoudigInformatieObjectenPageSize { get; set; } = 100;
    public int VerzendingenPageSize { get; set; } = 100;
    public bool IgnoreBusinessRuleDrc010 { get; set; }
    public bool IgnoreInformatieObjectTypeValidation { get; set; }
    public bool IgnoreZaakAndBesluitValidation { get; set; }
    public string DefaultDocumentenService { get; set; }
    public int UploadLargeDocumentChunkSizeMB { get; set; } = 128;
    public bool DocumentJobPrioritizationAtDownload { get; set; }
    public ExpandSettings ExpandSettings { get; set; } = new();
}
