using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.BusinessRules;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Referentielijsten.Contracts.v1;
using OneGround.ZGW.Referentielijsten.Contracts.v1.Responses;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.BusinessRulesTests;

public class ResultaatTypeBusinessRuleServiceTests
{
    private const string ProcesType = "https://example.test/procestypen/1";

    private readonly ResultaatTypeBusinessRuleService _service = new();

    private static ResultaatType CreateResultaatType(Afleidingswijze afleidingswijze) =>
        new() { BronDatumArchiefProcedure = new BronDatumArchiefProcedure { Afleidingswijze = afleidingswijze } };

    private static ZaakType CreateZaakType() => new() { SelectielijstProcestype = ProcesType };

    private static ResultaatResponseDto CreateResultaat(Procestermijn procesTermijn) =>
        new() { ProcesType = ProcesType, ProcesTermijn = procesTermijn };

    private async Task<(bool hasErrors, List<ValidationError> errors)> ValidateAsync(Afleidingswijze afleidingswijze, Procestermijn procesTermijn)
    {
        var errors = new List<ValidationError>();
        var hasErrors = await _service.ValidateAsync(CreateResultaatType(afleidingswijze), CreateZaakType(), CreateResultaat(procesTermijn), errors);
        return (hasErrors, errors);
    }

    [Theory]
    [InlineData(Afleidingswijze.afgehandeld)]
    [InlineData(Afleidingswijze.termijn)]
    [InlineData(Afleidingswijze.gerelateerde_zaak)]
    public async Task LeegProcesTermijn_AcceptsAnyAfleidingswijze(Afleidingswijze afleidingswijze)
    {
        var (hasErrors, errors) = await ValidateAsync(afleidingswijze, Procestermijn.leeg);

        Assert.False(hasErrors);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task Nihil_WithAfleidingswijzeAfgehandeld_IsValid()
    {
        var (hasErrors, errors) = await ValidateAsync(Afleidingswijze.afgehandeld, Procestermijn.nihil);

        Assert.False(hasErrors);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task Nihil_WithOtherAfleidingswijze_ReturnsValidationError()
    {
        var (hasErrors, errors) = await ValidateAsync(Afleidingswijze.termijn, Procestermijn.nihil);

        Assert.True(hasErrors);
        Assert.Contains(errors, e => e.Code == ErrorCode.InvalidAfleidingswijzeForProcesTermijn);
    }

    [Fact]
    public async Task IngeschatteBestaansduur_WithOtherAfleidingswijze_ReturnsValidationError()
    {
        var (hasErrors, errors) = await ValidateAsync(Afleidingswijze.afgehandeld, Procestermijn.ingeschatte_bestaansduur_procesobject);

        Assert.True(hasErrors);
        Assert.Contains(errors, e => e.Code == ErrorCode.InvalidAfleidingswijzeForProcesTermijn);
    }

    [Theory]
    [InlineData(Procestermijn.bestaansduur_procesobject)]
    [InlineData(Procestermijn.vast_te_leggen_datum)]
    [InlineData(Procestermijn.samengevoegd_met_bewaartermijn)]
    [InlineData(Procestermijn.na_afhandeling)]
    [InlineData(Procestermijn.ander_kenmerk_procesobject)]
    [InlineData(Procestermijn.na_einde_procesobject)]
    public async Task OtherProcesTermijn_DoesNotAddProcesTermijnErrors(Procestermijn procesTermijn)
    {
        var (_, errors) = await ValidateAsync(Afleidingswijze.afgehandeld, procesTermijn);

        Assert.DoesNotContain(errors, e => e.Code == ErrorCode.InvalidAfleidingswijzeForProcesTermijn);
    }
}
