using Roxit.ZGW.Zaken.Contracts.v1._5;

namespace Roxit.ZGW.Zaken.Web.Validators.v1._5;

public class PatchZaakValidationDto
{
    public Zaken.Contracts.v1.ZaakVerlengingDto Verlenging { get; set; }

    public Zaken.Contracts.v1.ZaakOpschortingDto Opschorting { get; set; }
    public ZaakProcessobjectDto Processobject { get; set; }

    public string Betalingsindicatie { get; set; }

    public string LaatsteBetaaldatum { get; set; }
}
