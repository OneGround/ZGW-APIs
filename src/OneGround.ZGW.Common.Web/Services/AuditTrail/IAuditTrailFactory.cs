namespace OneGround.ZGW.Common.Web.Services.AuditTrail;

public interface IAuditTrailFactory
{
    IAuditTrailService Create(AuditTrailOptions options, bool legacy = true);
    IAuditTrailService Create(AuditTrailOptions options, string name);
}
