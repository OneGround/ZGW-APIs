using FluentValidation;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._5.Queries;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Configuration;
using OneGround.ZGW.Zaken.Web.Validators.v1._5.Queries;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5;

// 1. ZaakSearchRequestDto is used in POST /api/v1/zaken/_zoek with serach criteria in the BODY
public class ZaakSearchRequestValidator : ZGWValidator<ZaakSearchRequestDto>
{
    public ZaakSearchRequestValidator(IConfiguration configuration)
    {
        var applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();

        CascadeRuleFor(p => p.Expand).ExpandsValid(SupportedExpands.GetAll("zaak")).IsExpandEnabled(applicationConfiguration.ExpandSettings.Search);

        // Add validation for the common search fields
        Include(new ZakenCommonSearchableFields());

        CascadeRuleFor(r => r.ZaakGeometry)
            .ChildRules(v =>
            {
                v.CascadeRuleFor(z => z.Within).NotNull();
            });

        // Note: The "<*>__in" search fields are array types
        CascadeRuleForEach(p => p.Archiefnominatie__in).IsEnumName(typeof(ArchiefNominatie)).WithName("archiefnominatie__in");
        CascadeRuleForEach(p => p.Archiefstatus__in).IsEnumName(typeof(ArchiefStatus)).WithName("archiefstatus__in");
        CascadeRuleForEach(p => p.Bronorganisatie__in).IsRsin().WithName("bronorganisatie__in");

        CascadeRuleForEach(p => p.Zaaktype__in).IsUri().WithName("zaaktype__in");
        CascadeRuleForEach(p => p.Uuid__in).IsGuid().WithName("uuid__in");
    }
}

// 2. GetAllZakenQueryParameters is used in the regular GET /api/v1/zaken?<query-parameters>
public class ZakenQueryParametersValidator : ZGWValidator<GetAllZakenQueryParameters>
{
    public ZakenQueryParametersValidator(IConfiguration configuration)
    {
        var applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();

        CascadeRuleFor(p => p.Expand).ExpandsValid(SupportedExpands.GetAll("zaak")).IsExpandEnabled(applicationConfiguration.ExpandSettings.List);

        // Add validation for the common search fields
        Include(new ZakenCommonSearchableFields());

        // Note: The "<*>__in" search fields are single strings with comma separated strings
        CascadeRuleForEach(p => TryList(p.Archiefnominatie__in)).IsEnumName(typeof(ArchiefNominatie)).WithName("archiefnominatie__in");
        CascadeRuleForEach(p => TryList(p.Archiefstatus__in)).IsEnumName(typeof(ArchiefStatus)).WithName("archiefstatus__in");
        CascadeRuleForEach(p => TryList(p.Bronorganisatie__in)).IsRsin().WithName("bronorganisatie__in");
    }
}
