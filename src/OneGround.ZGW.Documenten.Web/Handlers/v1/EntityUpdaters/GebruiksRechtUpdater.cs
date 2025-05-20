using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1.EntityUpdaters;

public class GebruiksRechtUpdater : IEntityUpdater<GebruiksRecht>
{
    public void Update(GebruiksRecht request, GebruiksRecht source, decimal version = 1)
    {
        source.OmschrijvingVoorwaarden = request.OmschrijvingVoorwaarden;
        source.Startdatum = request.Startdatum;
        source.Einddatum = request.Einddatum;
    }
}
