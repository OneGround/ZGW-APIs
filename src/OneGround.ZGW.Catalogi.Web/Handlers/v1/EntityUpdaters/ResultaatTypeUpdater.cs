using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Web;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1.EntityUpdaters;

public class ResultaatTypeUpdater : IEntityUpdater<ResultaatType>
{
    public void Update(ResultaatType request, ResultaatType source, decimal version = 1)
    {
        source.Omschrijving = request.Omschrijving;
        source.ResultaatTypeOmschrijving = request.ResultaatTypeOmschrijving;
        source.SelectieLijstKlasse = request.SelectieLijstKlasse;
        source.Toelichting = request.Toelichting;
        source.ArchiefNominatie = request.ArchiefNominatie;
        source.ArchiefActieTermijn = request.ArchiefActieTermijn;

        if (request.BronDatumArchiefProcedure != null)
        {
            source.BronDatumArchiefProcedure = new BronDatumArchiefProcedure
            {
                Afleidingswijze = request.BronDatumArchiefProcedure.Afleidingswijze,
                DatumKenmerk = request.BronDatumArchiefProcedure.DatumKenmerk,
                EindDatumBekend = request.BronDatumArchiefProcedure.EindDatumBekend,
                ObjectType = request.BronDatumArchiefProcedure.ObjectType,
                ProcesTermijn = request.BronDatumArchiefProcedure.ProcesTermijn,
                Registratie = request.BronDatumArchiefProcedure.Registratie,
            };
        }
        else
        {
            source.BronDatumArchiefProcedure = null;
        }

        // Note: Fields for v1.3 (check minimal version so it prevents from rewriting from older versions with default (null) values)
        if (version >= 1.3M)
        {
            source.ProcesObjectAard = source.ProcesObjectAard;
            source.IndicatieSpecifiek = source.IndicatieSpecifiek;
            source.ProcesTermijn = source.ProcesTermijn;
            // TODO: Not clear how to model the besluittypen and informatieobjecttypen collections (we asked VNG for). For now we don't update/store anything

            // Note: Derive from Zaaktype instead of getting from request (decided to do so)
            source.BeginGeldigheid = source.ZaakType.BeginGeldigheid;
            source.EindeGeldigheid = source.ZaakType.EindeGeldigheid;
            source.BeginObject = source.ZaakType.BeginObject;
            source.EindeObject = source.ZaakType.EindeObject;
            // ----
        }
    }
}
