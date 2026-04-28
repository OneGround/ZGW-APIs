using System;

namespace OneGround.ZGW.DataAccess.AuditTrail;

public interface IZgwAuditTrailRegel : IBaseEntity
{
    public string Url { get; }
    public string Bron { get; set; }
    public string ApplicatieId { get; set; }
    public string ApplicatieWeergave { get; set; }
    public string GebruikersId { get; set; }
    public string GebruikersWeergave { get; set; }
    public string Actie { get; set; }
    public string ActieWeergave { get; set; }
    public int Resultaat { get; set; }
    public string HoofdObject { get; set; }
    public string Resource { get; set; }
    public string ResourceUrl { get; set; }
    public string Toelichting { get; set; }
    public string ResourceWeergave { get; set; }
    public DateTime AanmaakDatum { get; set; }
    public string RequestId { get; set; }
    public Guid? HoofdObjectId { get; set; }
}
