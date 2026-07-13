using System.Collections.Generic;

namespace OneGround.ZGW.Common.Web.Configuration;

public class AuditTrailRetrieveOptions
{
    public const string SectionName = "Application";

    public bool AudittrailRecordRetrieveMinimal { get; set; } = true;
    public IList<string> AudittrailRetrieveRecordExcludeClientIds { get; set; } = [];
}
