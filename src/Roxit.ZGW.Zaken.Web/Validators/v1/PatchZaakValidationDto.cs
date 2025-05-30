using Roxit.ZGW.Zaken.Contracts.v1;

namespace Roxit.ZGW.Zaken.Web.Validators.v1;

public class PatchZaakValidationDto
{
    public ZaakVerlengingDto Verlenging { get; set; }

    public ZaakOpschortingDto Opschorting { get; set; }

    public string Betalingsindicatie { get; set; }

    public string LaatsteBetaaldatum { get; set; }
}
