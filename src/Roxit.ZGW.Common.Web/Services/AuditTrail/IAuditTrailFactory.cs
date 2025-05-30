namespace Roxit.ZGW.Common.Web.Services.AuditTrail;

public interface IAuditTrailFactory
{
    IAuditTrailService Create(AuditTrailOptions options);
}
