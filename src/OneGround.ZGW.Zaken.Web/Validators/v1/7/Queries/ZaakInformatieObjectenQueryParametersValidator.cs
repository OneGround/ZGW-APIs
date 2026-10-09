using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._7.Queries;

// Note: Mirrors v1._5.Queries.ZaakInformatieObjectenQueryParametersValidator for the shared filter fields (that one validates the v1 type,
// the v1._7 query parameters are a type of their own because of "expand"). Without it an invalid "zaak"/"informatieobject" url would not be
// answered with a 400 but would blow up in the handler when the resource id is taken from it.
// Unlike v1._5, "expand" is not validated here -- for v1.7, expand is validated by the new ExpandValidator<T> in the controller instead
// (see Controllers/v1/7/ZaakInformatieObjectenController.cs).
public class ZaakInformatieObjectenQueryParametersValidator : ZGWValidator<GetAllZaakInformatieObjectenQueryParameters>
{
    public ZaakInformatieObjectenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Zaak).IsUri();
        CascadeRuleFor(p => p.InformatieObject).IsUri();
    }
}
