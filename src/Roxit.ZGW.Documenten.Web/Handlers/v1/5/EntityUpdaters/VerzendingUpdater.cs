using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Documenten.DataModel;

namespace Roxit.ZGW.Documenten.Web.Handlers.v1._5.EntityUpdaters;

public class VerzendingUpdater : IEntityUpdater<Verzending>
{
    public void Update(Verzending request, Verzending source, decimal version = 1)
    {
        source.Betrokkene = request.Betrokkene;
        source.AardRelatie = request.AardRelatie;
        source.Toelichting = request.Toelichting;
        source.Ontvangstdatum = request.Ontvangstdatum;
        source.Verzenddatum = request.Verzenddatum;
        source.Contactpersoon = request.Contactpersoon;
        source.ContactpersoonNaam = request.ContactpersoonNaam;
        source.BinnenlandsCorrespondentieAdres = request.BinnenlandsCorrespondentieAdres;
        source.BuitenlandsCorrespondentieAdres = request.BuitenlandsCorrespondentieAdres;
        source.CorrespondentiePostadres = request.CorrespondentiePostadres;
        source.Faxnummer = request.Faxnummer;
        source.EmailAdres = request.EmailAdres;
        source.MijnOverheid = request.MijnOverheid;
        source.Telefoonnummer = request.Telefoonnummer;
    }
}
