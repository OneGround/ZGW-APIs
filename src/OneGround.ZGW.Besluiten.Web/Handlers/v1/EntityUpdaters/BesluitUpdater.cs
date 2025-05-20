using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Common.Web;

namespace OneGround.ZGW.Besluiten.Web.Handlers.v1.EntityUpdaters;

public class BesluitUpdater : IEntityUpdater<Besluit>
{
    public void Update(Besluit request, Besluit source, decimal version = 1)
    {
        source.Zaak = request.Zaak;
        source.Datum = request.Datum;
        source.Toelichting = request.Toelichting;
        source.BestuursOrgaan = request.BestuursOrgaan;
        source.IngangsDatum = request.IngangsDatum;
        source.VervalDatum = request.VervalDatum;
        source.VervalReden = request.VervalReden;
        source.PublicatieDatum = request.PublicatieDatum;
        source.VerzendDatum = request.VerzendDatum;
        source.UiterlijkeReactieDatum = request.UiterlijkeReactieDatum;
    }
}
