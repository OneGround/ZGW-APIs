using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Zaken.Contracts.v1.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.ServiceAgent.v1;
using OneGround.ZGW.Zaken.Web.Services;
using OneGround.ZGW.Zaken.Web.Services.BronDate;
using Xunit;
using static OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests.Authorization.ZrcAuthorizationTestSupport;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.BronDateTests;

public class GerelateerdeZaakBronDateTests : IDisposable
{
    private const string ConfiguredZrc = "https://zrc.configured.test/zaken/api/v1";

    private readonly ZrcDbContext _context = CreateDbContext();
    private readonly Mock<IZakenServiceAgent> _zakenServiceAgent = new();
    private readonly Mock<IEntityUriService> _uriService = new();

    public GerelateerdeZaakBronDateTests()
    {
        // The configured ZRC address (service discovery) is not the address of the current request
        _uriService
            .Setup(u => u.GetUri(ServiceRoleName.ZRC, It.IsAny<IUrlEntity>()))
            .Returns<string, IUrlEntity>((_, entity) => $"{ConfiguredZrc}{entity.Url}");
        _uriService.Setup(u => u.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(entity => $"https://other-request-host.test{entity.Url}");
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task The_related_zaak_is_looked_up_on_the_configured_zrc_address_and_the_latest_einddatum_is_used()
    {
        var (zaak, related1, related2) = await SeedZaakWithTwoRelatedZaken();
        _zakenServiceAgent
            .Setup(a => a.GetZaakByUrlAsync($"{ConfiguredZrc}/zaken/{related1}"))
            .ReturnsAsync(new ServiceAgentResponse<ZaakResponseDto>(new ZaakResponseDto { Einddatum = "2026-03-01" }));
        _zakenServiceAgent
            .Setup(a => a.GetZaakByUrlAsync($"{ConfiguredZrc}/zaken/{related2}"))
            .ReturnsAsync(new ServiceAgentResponse<ZaakResponseDto>(new ZaakResponseDto { Einddatum = "2026-05-15" }));
        var errors = new List<ArchiveValidationError>();

        var date = await CreateService().GetAsync(zaak, errors, CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 5, 15), date);
        Assert.Empty(errors);
        _zakenServiceAgent.Verify(a => a.GetZaakByUrlAsync(It.Is<string>(u => u.StartsWith("https://other-request-host.test"))), Times.Never);
    }

    [Fact]
    public async Task A_failed_lookup_is_reported_instead_of_throwing()
    {
        var (zaak, _, _) = await SeedZaakWithTwoRelatedZaken();
        var notFound = new ErrorResponse
        {
            Code = "not-found",
            Title = "Niet gevonden",
            Status = 404,
        };
        _zakenServiceAgent.Setup(a => a.GetZaakByUrlAsync(It.IsAny<string>())).ReturnsAsync(new ServiceAgentResponse<ZaakResponseDto>(notFound));
        var errors = new List<ArchiveValidationError>();

        var date = await CreateService().GetAsync(zaak, errors, CancellationToken.None);

        Assert.Null(date);
        Assert.NotEmpty(errors);
    }

    private async Task<(Zaak Zaak, Guid Related1, Guid Related2)> SeedZaakWithTwoRelatedZaken()
    {
        var zaak = NewZaak(CurrentTenant, ZaakTypeA);
        var related1 = Guid.NewGuid();
        var related2 = Guid.NewGuid();
        _context.Zaken.Add(zaak);
        _context.RelevanteAndereZaken.AddRange(
            new RelevanteAndereZaak
            {
                Id = Guid.NewGuid(),
                Owner = CurrentTenant,
                ZaakId = zaak.Id,
                GerelateerdeZaakId = related1,
                AardRelatie = "vervolg",
            },
            new RelevanteAndereZaak
            {
                Id = Guid.NewGuid(),
                Owner = CurrentTenant,
                ZaakId = zaak.Id,
                GerelateerdeZaakId = related2,
                AardRelatie = "bijdrage",
            }
        );
        await _context.SaveChangesAsync();
        return (zaak, related1, related2);
    }

    private GerelateerdeZaakBronDate CreateService() => new(_zakenServiceAgent.Object, _context, _uriService.Object);
}
