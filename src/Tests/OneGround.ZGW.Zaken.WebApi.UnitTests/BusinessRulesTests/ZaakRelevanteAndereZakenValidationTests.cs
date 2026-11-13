using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.ServiceAgent.v1._7;
using OneGround.ZGW.Notificaties.ServiceAgent;
using OneGround.ZGW.Zaken.Contracts.v1.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.ServiceAgent.v1;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using Xunit;
using static OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests.Authorization.ZrcAuthorizationTestSupport;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.BusinessRulesTests;

/// <summary>
/// The relevanteAndereZaken urls of a request are validated as the client sent them (the relation itself is stored by the id of the zaak only).
/// </summary>
public class ZaakRelevanteAndereZakenValidationTests : IDisposable
{
    private const string ZaakUrl = "http://zaken.local/api/v1/zaken/eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
    private static readonly Guid ZaakId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private const string RequestHostUrl = "https://request-host.test/zaken/api/v1/zaken/eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";

    private readonly ZrcDbContext _context = CreateDbContext();
    private readonly Mock<IZakenServiceAgent> _zakenServiceAgent = new();

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task A_url_of_an_existing_zaak_is_accepted_and_fetched_as_sent()
    {
        _zakenServiceAgent.Setup(a => a.GetZaakByUrlAsync(ZaakUrl)).ReturnsAsync(new ServiceAgentResponse<ZaakResponseDto>(new ZaakResponseDto()));
        var errors = new List<ValidationError>();

        var valid = await Validate([ZaakUrl], errors);

        Assert.True(valid);
        Assert.Empty(errors);
        _zakenServiceAgent.Verify(a => a.GetZaakByUrlAsync(ZaakUrl), Times.Once);
    }

    [Theory]
    [InlineData("http://zaken.local/api/v1/zaken/no-guid")]
    [InlineData("http://zaken.local/api/v1/zaken/eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee/rollen/ffffffff-ffff-ffff-ffff-ffffffffffff")]
    [InlineData("http://zaken.local/api/v1/enkelvoudiginformatieobjecten/eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee")]
    public async Task A_url_that_does_not_contain_a_zaak_id_is_rejected_and_named_in_the_error(string url)
    {
        // The service agent accepts every url here; it is the id check that has to reject the url (the first url in the list is valid)
        _zakenServiceAgent
            .Setup(a => a.GetZaakByUrlAsync(It.IsAny<string>()))
            .ReturnsAsync(new ServiceAgentResponse<ZaakResponseDto>(new ZaakResponseDto()));
        var errors = new List<ValidationError>();

        var valid = await Validate([ZaakUrl, url], errors);

        Assert.False(valid);
        var error = Assert.Single(errors);
        Assert.Equal("relevanteAndereZaken.1.url", error.Name);
        Assert.Equal(ErrorCode.InvalidResource, error.Code);
        Assert.Contains(url, error.Reason);
    }

    [Fact]
    public async Task An_empty_url_is_rejected_without_calling_the_service_agent()
    {
        var errors = new List<ValidationError>();

        var valid = await Validate([""], errors);

        Assert.False(valid);
        var error = Assert.Single(errors);
        Assert.Equal("relevanteAndereZaken.0.url", error.Name);
        Assert.Equal(ErrorCode.Required, error.Code);
        _zakenServiceAgent.Verify(a => a.GetZaakByUrlAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task No_urls_means_nothing_to_validate()
    {
        var errors = new List<ValidationError>();

        Assert.True(await Validate(null, errors));
        Assert.Empty(errors);
        _zakenServiceAgent.Verify(a => a.GetZaakByUrlAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task A_relation_the_zaak_already_has_is_not_validated_again_whatever_the_address_of_its_url()
    {
        // On an update the url of an existing relation is rebuilt from the stored id on the address of the request; the service agent would reject it
        var notAccepted = new ErrorResponse
        {
            Code = "bad-url",
            Title = "De URL is van een externe Zaken API",
            Status = 400,
        };
        _zakenServiceAgent.Setup(a => a.GetZaakByUrlAsync(It.IsAny<string>())).ReturnsAsync(new ServiceAgentResponse<ZaakResponseDto>(notAccepted));
        var errors = new List<ValidationError>();

        var valid = await Validate([RequestHostUrl], errors, ZaakId);

        Assert.True(valid);
        Assert.Empty(errors);
        _zakenServiceAgent.Verify(a => a.GetZaakByUrlAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task A_new_relation_is_validated_next_to_an_existing_one()
    {
        const string newZaakUrl = "http://zaken.local/api/v1/zaken/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
        var notFound = new ErrorResponse
        {
            Code = "not-found",
            Title = "Niet gevonden",
            Status = 404,
        };
        _zakenServiceAgent.Setup(a => a.GetZaakByUrlAsync(newZaakUrl)).ReturnsAsync(new ServiceAgentResponse<ZaakResponseDto>(notFound));
        var errors = new List<ValidationError>();

        var valid = await Validate([RequestHostUrl, newZaakUrl], errors, ZaakId);

        Assert.False(valid);
        var error = Assert.Single(errors);
        Assert.Equal("relevanteAndereZaken.1.url", error.Name);
        Assert.Equal("not-found", error.Code);
        _zakenServiceAgent.Verify(a => a.GetZaakByUrlAsync(RequestHostUrl), Times.Never);
    }

    // Archiefstatus is set explicitly: any other value makes the rule load the (unseeded) existing zaak.
    private Task<bool> Validate(IEnumerable<string> relevanteAndereZaakUrls, List<ValidationError> errors, params Guid[] existingRelatedZaakIds)
    {
        var existing = NewZaak(CurrentTenant, ZaakTypeA);
        existing.RelevanteAndereZaken = existingRelatedZaakIds
            .Select(id => new RelevanteAndereZaak
            {
                Id = Guid.NewGuid(),
                GerelateerdeZaakId = id,
                AardRelatie = "vervolg",
            })
            .ToList();
        var update = new Zaak
        {
            Zaaktype = ZaakTypeA,
            Archiefstatus = ArchiefStatus.nog_te_archiveren,
            RelevanteAndereZaken = [],
        };

        var service = new ZaakBusinessRuleService(
            _context,
            Mock.Of<IEntityUriService>(),
            Mock.Of<ICatalogiServiceAgent>(),
            _zakenServiceAgent.Object,
            Mock.Of<IDocumentenServiceAgent>(),
            Mock.Of<INotificatiesServiceAgent>()
        );

        return service.ValidateAsync(existing, update, null, relevanteAndereZaakUrls, errors);
    }
}
