using FluentValidation;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1.Queries;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Validators.v1.Queries;

public class ZakenQueryParametersValidator : ZGWValidator<GetAllZakenQueryParameters>
{
    public ZakenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Bronorganisatie).IsRsin(required: false);
        CascadeRuleFor(p => p.Zaaktype).IsUri();
        CascadeRuleFor(p => p.Archiefnominatie).IsEnumName(typeof(ArchiefNominatie));
        CascadeRuleFor(p => p.Archiefactiedatum).IsDate(false);
        CascadeRuleFor(p => p.Archiefactiedatum__lt).IsDate(false);
        CascadeRuleFor(p => p.Archiefactiedatum__gt).IsDate(false);
        CascadeRuleFor(p => p.Archiefstatus).IsEnumName(typeof(ArchiefStatus));
        CascadeRuleFor(p => p.Startdatum).IsDate(false);
        CascadeRuleFor(p => p.Startdatum__gte).IsDate(false);
        CascadeRuleFor(p => p.Startdatum__gt).IsDate(false);
        CascadeRuleFor(p => p.Startdatum__lte).IsDate(false);
        CascadeRuleFor(p => p.Startdatum__lt).IsDate(false);
    }
}
