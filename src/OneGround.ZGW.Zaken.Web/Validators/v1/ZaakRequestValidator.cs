using FluentValidation;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1.Requests;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Validators.v1;

public class ZaakRequestValidator : ZGWValidator<ZaakRequestDto>
{
    public ZaakRequestValidator()
    {
        CascadeRuleFor(z => z.Identificatie).MaximumLength(40);
        CascadeRuleFor(z => z.Bronorganisatie).IsRsin(true);
        CascadeRuleFor(z => z.Omschrijving).MaximumLength(80);
        CascadeRuleFor(z => z.Toelichting).MaximumLength(1000);
        CascadeRuleFor(z => z.Zaaktype).NotNull().NotEmpty().IsUri().MaximumLength(1000);
        CascadeRuleFor(z => z.Registratiedatum).IsDate(false);
        CascadeRuleFor(z => z.VerantwoordelijkeOrganisatie).IsRsin(true);
        CascadeRuleFor(z => z.Startdatum).IsDate(true);
        CascadeRuleFor(z => z.EinddatumGepland).IsDate(false);
        CascadeRuleFor(z => z.UiterlijkeEinddatumAfdoening).IsDate(false);
        CascadeRuleFor(z => z.Publicatiedatum).IsDate(false);
        CascadeRuleFor(z => z.Communicatiekanaal).NotNull().IsUri().MaximumLength(1000);
        CascadeRuleForEach(z => z.ProductenOfDiensten).IsUri();
        CascadeRuleFor(z => z.Vertrouwelijkheidaanduiding).IsEnumName(typeof(VertrouwelijkheidAanduiding));
        CascadeRuleFor(z => z.Betalingsindicatie)
            .IsEnumName(typeof(BetalingsIndicatie))
            .When(zaak => !string.IsNullOrWhiteSpace(zaak.Betalingsindicatie));
        CascadeRuleFor(z => z.LaatsteBetaaldatum).IsDateTime().NotInTheFuture();
        When(
            z => z.Verlenging != null,
            () =>
            {
                CascadeRuleFor(z => z.Verlenging.Reden).NotNull().NotEmpty().MaximumLength(200).OverridePropertyName("verlenging.reden");
                CascadeRuleFor(z => z.Verlenging.Duur).NotNull().NotEmpty().IsDuration().OverridePropertyName("verlenging.duur");
            }
        );
        When(
            z => z.Opschorting != null,
            () =>
            {
                CascadeRuleFor(z => z.Opschorting.Indicatie).NotNull().OverridePropertyName("opschorting.indicatie");
                CascadeRuleFor(z => z.Opschorting.Reden).NotNull().NotEmpty().MaximumLength(200).OverridePropertyName("opschorting.reden");
            }
        );
        CascadeRuleFor(z => z.Selectielijstklasse).NotNull().IsUri().MaximumLength(1000);
        CascadeRuleFor(z => z.Hoofdzaak).IsUri();
        CascadeRuleForEach(z => z.RelevanteAndereZaken)
            .ChildRules(v =>
            {
                v.CascadeRuleFor(r => r.Url).NotNull().NotEmpty().IsUri().MaximumLength(1000);
                v.CascadeRuleFor(r => r.AardRelatie).NotNull().NotEmpty().IsEnumName(typeof(AardRelatie));
            });
        CascadeRuleForEach(z => z.Kenmerken)
            .ChildRules(v =>
            {
                v.CascadeRuleFor(k => k.Kenmerk).NotNull().NotEmpty().MaximumLength(40);
                v.CascadeRuleFor(k => k.Bron).NotNull().NotEmpty().MaximumLength(40);
            });
        CascadeRuleFor(z => z.Archiefnominatie).IsEnumName(typeof(ArchiefNominatie));
        CascadeRuleFor(z => z.Archiefstatus).IsEnumName(typeof(ArchiefStatus));
        CascadeRuleFor(z => z.Archiefactiedatum).IsDate(false);
    }
}
