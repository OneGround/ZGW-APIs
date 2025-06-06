using OneGround.ZGW.Catalogi.Contracts.v1._3.Queries;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Catalogi.Web.Validators.v1._3.Queries;

public class ZaakObjectTypenQueryParametersValidator : ZGWValidator<GetAllZaakObjectTypenQueryParameters>
{
    public ZaakObjectTypenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.AnderObjectType).IsBoolean();
        CascadeRuleFor(p => p.Catalogus).IsUri();
        CascadeRuleFor(p => p.DatumBeginGeldigheid).IsDate(required: false);
        CascadeRuleFor(p => p.DatumEindeGeldigheid).IsDate(required: false);
        CascadeRuleFor(p => p.DatumGeldigheid).IsDate(required: false);
        CascadeRuleFor(p => p.ObjectType).IsUri();
        CascadeRuleFor(p => p.ZaakType).IsUri();
    }
}
