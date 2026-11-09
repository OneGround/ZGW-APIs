using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Gesprekken.Contracts.v1;
using OneGround.ZGW.Gesprekken.Contracts.v1.Requests;
using OneGround.ZGW.Gesprekken.Web.Helpers;
using static OneGround.ZGW.Gesprekken.Web.Validators.BijdrageRegels;

namespace OneGround.ZGW.Gesprekken.Web.Validators;

public class BijlageValidator : ZGWValidator<BijlageDto>
{
    public BijlageValidator()
    {
        CascadeRuleFor(b => b.Bestand).NotNull().NotEmpty().IsGrcUrl(GrcUrl.Bestanden);
    }
}

public static class BijdrageRegels
{
    public const int MaxTekstLengte = 5000;
}

public class EersteGespreksbijdrageRequestValidator : ZGWValidator<EersteGespreksbijdrageRequestDto>
{
    public EersteGespreksbijdrageRequestValidator()
    {
        CascadeRuleFor(b => b.TekstGespreksBijdrage).NotNull().NotEmpty().MaximumLength(MaxTekstLengte);
        RuleForEach(b => b.Bijlagen).SetValidator(new BijlageValidator());
        CascadeRuleFor(b => b.Verzender).NotNull();
        // The gesprek does not exist yet, so the verzender is always a new deelname.
        CascadeRuleFor(b => b.Verzender.NieuweDeelname)
            .NotNull()
            .SetValidator(new DeelnameRequestValidator())
            .When(b => b.Verzender != null)
            .OverridePropertyName("verzender.nieuweDeelname");
        CascadeRuleFor(b => b.Verzender.Gespreksdeelname)
            .Empty()
            .WithMessage("Bij een nieuw gesprek bestaat nog geen gespreksdeelname; gebruik nieuweDeelname.")
            .When(b => b.Verzender != null)
            .OverridePropertyName("verzender.gespreksdeelname");
    }
}

public class GespreksbijdrageRequestValidator : ZGWValidator<GespreksbijdrageRequestDto>
{
    public GespreksbijdrageRequestValidator()
    {
        CascadeRuleFor(b => b.TekstGespreksBijdrage).NotNull().NotEmpty().MaximumLength(MaxTekstLengte);
        CascadeRuleFor(b => b.Gesprek).NotNull().NotEmpty().IsGrcUrl(GrcUrl.Gesprekken);
        CascadeRuleFor(b => b.IsReactieOp).IsGrcUrl(GrcUrl.Gespreksbijdragen);
        RuleForEach(b => b.Bijlagen).SetValidator(new BijlageValidator());
        CascadeRuleFor(b => b.Verzender).NotNull();
        CascadeRuleFor(b => b.Verzender)
            .Must(v => string.IsNullOrEmpty(v.Gespreksdeelname) != (v.NieuweDeelname == null))
            .WithMessage("Geef precies één van gespreksdeelname of nieuweDeelname.")
            .When(b => b.Verzender != null);
        CascadeRuleFor(b => b.Verzender.Gespreksdeelname)
            .IsGrcUrl(GrcUrl.Gespreksdeelnames)
            .When(b => b.Verzender != null && !string.IsNullOrEmpty(b.Verzender.Gespreksdeelname))
            .OverridePropertyName("verzender.gespreksdeelname");
        CascadeRuleFor(b => b.Verzender.NieuweDeelname)
            .SetValidator(new DeelnameRequestValidator())
            .When(b => b.Verzender?.NieuweDeelname != null)
            .OverridePropertyName("verzender.nieuweDeelname");
    }
}

public class GesprekRequestValidator : ZGWValidator<GesprekRequestDto>
{
    public GesprekRequestValidator()
    {
        CascadeRuleFor(g => g.Gespreksonderwerp).NotNull().NotEmpty().MaximumLength(200);
        CascadeRuleFor(g => g.AanleidingZaak).IsUri().MaximumLength(1000);
        CascadeRuleFor(g => g.AanleidingThema).IsUri().MaximumLength(1000);
        CascadeRuleFor(g => g.AanleidingProductDienst).IsUri().MaximumLength(1000);
        CascadeRuleFor(g => g.AanleidingAgendaAfspraak).IsUri().MaximumLength(1000);
        CascadeRuleFor(g => g.AanleidingPlan).IsUri().MaximumLength(1000);
        CascadeRuleFor(g => g.Ontvanger).NotNull().NotEmpty();
        RuleForEach(g => g.Ontvanger).SetValidator(new DeelnameRequestValidator());
        CascadeRuleFor(g => g.EersteBijdrage).NotNull().SetValidator(new EersteGespreksbijdrageRequestValidator());
    }
}

public class GelezenGespreksbijdrageRequestValidator : ZGWValidator<GelezenGespreksbijdrageRequestDto>
{
    public GelezenGespreksbijdrageRequestValidator()
    {
        CascadeRuleFor(g => g.Gespreksdeelname).NotNull().NotEmpty().IsGrcUrl(GrcUrl.Gespreksdeelnames);
    }
}

public class UploadSessieRequestValidator : ZGWValidator<UploadSessieRequestDto>
{
    public UploadSessieRequestValidator()
    {
        CascadeRuleFor(u => u.Bestandsnaam).NotNull().NotEmpty().MaximumLength(255);
        CascadeRuleFor(u => u.Bestandsomvang).GreaterThan(0);
        CascadeRuleFor(u => u.ContentType).MaximumLength(100);
        CascadeRuleFor(u => u.Titel).MaximumLength(200);
    }
}

public class RedactieRequestValidator : ZGWValidator<RedactieRequestDto>
{
    public RedactieRequestValidator()
    {
        CascadeRuleFor(r => r.Reden).NotNull().NotEmpty().MaximumLength(1000);
    }
}
