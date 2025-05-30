using FluentValidation;
using Roxit.ZGW.Catalogi.Contracts.v1.Requests;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Catalogi.Web.Validators.v1;

public class InformatieObjectTypeRequestValidator : ZGWValidator<InformatieObjectTypeRequestDto>
{
    public InformatieObjectTypeRequestValidator()
    {
        CascadeRuleFor(r => r.Catalogus).NotNull().IsUri();
        CascadeRuleFor(r => r.Omschrijving).NotNull().NotEmpty().MaximumLength(80);
        CascadeRuleFor(r => r.VertrouwelijkheidAanduiding).NotNull().IsEnumName(typeof(VertrouwelijkheidAanduiding));
        CascadeRuleFor(r => r.BeginGeldigheid).IsDate(true);
        CascadeRuleFor(r => r.EindeGeldigheid).IsDate(false);
    }
}
