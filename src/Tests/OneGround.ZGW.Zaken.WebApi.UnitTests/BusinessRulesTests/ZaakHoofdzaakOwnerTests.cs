using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.ServiceAgent.v1._7;
using OneGround.ZGW.Notificaties.ServiceAgent;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.ServiceAgent.v1;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using Xunit;
using static OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests.Authorization.ZrcAuthorizationTestSupport;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.BusinessRulesTests;

public class ZaakHoofdzaakOwnerTests : IDisposable
{
    private const string HoofdzaakUrl = "http://zaken.local/api/v1/zaken/eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";

    private readonly ZrcDbContext _context = CreateDbContext();
    private readonly Mock<IEntityUriService> _uriService = new();

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Update_with_a_hoofdzaak_of_another_organisation_is_rejected_as_non_existing()
    {
        await SeedHoofdzaak(OtherTenant);
        var existing = NewZaak(CurrentTenant, ZaakTypeA);
        var update = NewUpdate();
        var errors = new List<ValidationError>();

        var valid = await CreateService().ValidateAsync(existing, update, HoofdzaakUrl, errors);

        Assert.False(valid);
        var error = Assert.Single(errors);
        Assert.Equal("hoofdzaak", error.Name);
        Assert.Equal(ErrorCode.NoMatch, error.Code);
    }

    [Fact]
    public async Task Create_with_a_hoofdzaak_of_another_organisation_is_rejected_as_non_existing()
    {
        await SeedHoofdzaak(OtherTenant);
        var add = new Zaak
        {
            Owner = CurrentTenant,
            Zaaktype = ZaakTypeA,
            Archiefstatus = ArchiefStatus.nog_te_archiveren,
            RelevanteAndereZaken = [],
        };
        var errors = new List<ValidationError>();

        var valid = await CreateService().ValidateAsync(add, HoofdzaakUrl, ignoreZaakTypeValidation: true, errors);

        Assert.False(valid);
        Assert.Contains(errors, e => e.Name == "hoofdzaak" && e.Code == ErrorCode.NoMatch);
    }

    [Fact]
    public async Task Update_with_a_hoofdzaak_of_the_same_organisation_is_accepted()
    {
        await SeedHoofdzaak(CurrentTenant);
        var existing = NewZaak(CurrentTenant, ZaakTypeA);
        var update = NewUpdate();
        var errors = new List<ValidationError>();

        var valid = await CreateService().ValidateAsync(existing, update, HoofdzaakUrl, errors);

        Assert.True(valid);
        Assert.Empty(errors);
    }

    // Archiefstatus is set explicitly: any other value makes the rule load the (unseeded) existing zaak.
    private static Zaak NewUpdate() =>
        new()
        {
            Zaaktype = ZaakTypeA,
            Archiefstatus = ArchiefStatus.nog_te_archiveren,
            RelevanteAndereZaken = [],
        };

    private async Task SeedHoofdzaak(string owner)
    {
        var hoofdzaak = NewZaak(owner, ZaakTypeB);
        _context.Zaken.Add(hoofdzaak);
        await _context.SaveChangesAsync();
        _uriService.Setup(u => u.GetId(HoofdzaakUrl)).Returns(hoofdzaak.Id);
    }

    private ZaakBusinessRuleService CreateService() =>
        new(
            _context,
            _uriService.Object,
            Mock.Of<ICatalogiServiceAgent>(),
            Mock.Of<IZakenServiceAgent>(),
            Mock.Of<IDocumentenServiceAgent>(),
            Mock.Of<INotificatiesServiceAgent>()
        );
}
