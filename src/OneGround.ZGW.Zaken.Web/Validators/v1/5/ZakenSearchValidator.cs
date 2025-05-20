using FluentValidation;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._5;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5;

public class ZakenSearchValidator : ZGWValidator<IZakenSearchableFields>
{
    public ZakenSearchValidator()
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
        CascadeRuleForEach(p => TryList(p.Archiefnominatie__in)).IsEnumName(typeof(ArchiefNominatie)).WithName("archiefnominatie__in");
        CascadeRuleFor(p => p.Archiefactiedatum__lt).IsDate(false);
        CascadeRuleFor(p => p.Archiefactiedatum__gt).IsDate(false);
        CascadeRuleForEach(p => TryList(p.Archiefstatus__in)).IsEnumName(typeof(ArchiefStatus)).WithName("archiefstatus__in");
        CascadeRuleForEach(p => TryList(p.Bronorganisatie__in)).IsRsin().WithName("bronorganisatie__in");
        CascadeRuleFor(p => p.Archiefactiedatum__isnull).IsBoolean();
        CascadeRuleFor(p => p.Registratiedatum).IsDate(false);
        CascadeRuleFor(p => p.Registratiedatum__gt).IsDate(false);
        CascadeRuleFor(p => p.Registratiedatum__lt).IsDate(false);
        CascadeRuleFor(p => p.Einddatum).IsDate(false);
        CascadeRuleFor(p => p.Einddatum__isnull).IsBoolean();
        CascadeRuleFor(p => p.Einddatum__gt).IsDate(false);
        CascadeRuleFor(p => p.Einddatum__lt).IsDate(false);
        CascadeRuleFor(p => p.EinddatumGepland).IsDate(false);
        CascadeRuleFor(p => p.EinddatumGepland__gt).IsDate(false);
        CascadeRuleFor(p => p.EinddatumGepland__lt).IsDate(false);
        CascadeRuleFor(p => p.UiterlijkeEinddatumAfdoening).IsDate(false);
        CascadeRuleFor(p => p.UiterlijkeEinddatumAfdoening__gt).IsDate(false);
        CascadeRuleFor(p => p.UiterlijkeEinddatumAfdoening__lt).IsDate(false);
        CascadeRuleFor(p => p.Rol__betrokkeneType).IsEnumName(typeof(BetrokkeneType));
        CascadeRuleFor(p => p.Rol__betrokkene).IsUri();
        CascadeRuleFor(p => p.Rol__omschrijvingGeneriek).IsEnumName(typeof(OmschrijvingGeneriek));
        CascadeRuleFor(p => p.MaximaleVertrouwelijkheidaanduiding).IsEnumName(typeof(VertrouwelijkheidAanduiding));
        CascadeRuleFor(p => p.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn).IsRsin(required: false);
        CascadeRuleFor(p => p.Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie).MaximumLength(17);
        CascadeRuleFor(p => p.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer).MaximumLength(10);
        CascadeRuleFor(p => p.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId).MaximumLength(9);
        CascadeRuleFor(p => p.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie).MaximumLength(17);
        CascadeRuleFor(p => p.Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer).MaximumLength(24);
        CascadeRuleFor(p => p.Rol__betrokkeneIdentificatie__medewerker__identificatie).MaximumLength(24);
        CascadeRuleFor(p => p.Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie).MaximumLength(24);
    }
}
