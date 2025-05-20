using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Web;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1.EntityUpdaters;

public class EigenschapUpdater : IEntityUpdater<Eigenschap>
{
    public void Update(Eigenschap request, Eigenschap source, decimal version = 1)
    {
        source.Naam = request.Naam;
        source.Definitie = request.Definitie;
        source.Toelichting = request.Toelichting;

        if (request.Specificatie != null)
        {
            source.Specificatie = new EigenschapSpecificatie
            {
                Formaat = request.Specificatie.Formaat,
                Groep = request.Specificatie.Groep,
                Kardinaliteit = request.Specificatie.Kardinaliteit,
                Lengte = request.Specificatie.Lengte,
                Waardenverzameling = request.Specificatie.Waardenverzameling,
                Owner = source.Owner,
            };
        }
        else
        {
            source.Specificatie = null;
        }

        // Note: Fields for v1.3 (check minimal version so it prevents from rewriting from older versions with default (null) values)
        if (version >= 1.3M)
        {
            // Note: Derive from Zaaktype instead of getting from request (decided to do so)
            source.BeginGeldigheid = source.ZaakType.BeginGeldigheid;
            source.EindeGeldigheid = source.ZaakType.EindeGeldigheid;
            source.BeginObject = source.ZaakType.BeginObject;
            source.EindeObject = source.ZaakType.EindeObject;
            // ----
        }
    }
}
