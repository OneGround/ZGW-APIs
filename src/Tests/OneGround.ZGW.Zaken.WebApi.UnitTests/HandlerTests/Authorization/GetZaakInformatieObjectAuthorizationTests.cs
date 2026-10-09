using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.DataModel;
using Xunit;
using static OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests.Authorization.ZrcAuthorizationTestSupport;
using V1 = OneGround.ZGW.Zaken.Web.Handlers.v1;
using V1_5 = OneGround.ZGW.Zaken.Web.Handlers.v1._5;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests.Authorization;

public class GetZaakInformatieObjectAuthorizationTests : IDisposable
{
    private static readonly string[] ReadScopes = [AuthorizationScopes.Zaken.Read];
    private static readonly string[] PatchScopes = [AuthorizationScopes.Zaken.Update, AuthorizationScopes.Zaken.ForcedUpdate];

    private readonly ZrcDbContext _context = CreateDbContext();

    public void Dispose() => _context.Dispose();

    [Theory]
    [InlineData("v1")]
    [InlineData("v1.5")]
    public async Task ZaakInformatieObject_of_a_zaaktype_the_client_is_not_authorized_for_is_forbidden(string version)
    {
        var id = await SeedZaakInformatieObjectAsync(CurrentTenant, ZaakTypeA);
        var client = Client(ReadScopes, Permission(ZaakTypeB, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Read));

        var result = await GetAsync(version, id, client);

        Assert.Equal(QueryStatus.Forbidden, result.Status);
        Assert.Null(result.Result);
    }

    [Theory]
    [InlineData("v1")]
    [InlineData("v1.5")]
    public async Task ZaakInformatieObject_of_an_authorized_zaaktype_is_returned(string version)
    {
        var id = await SeedZaakInformatieObjectAsync(CurrentTenant, ZaakTypeA);
        var client = Client(ReadScopes, Permission(ZaakTypeA, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Read));

        var result = await GetAsync(version, id, client);

        Assert.Equal(QueryStatus.OK, result.Status);
        Assert.Equal(id, result.Result.Id);
    }

    [Theory]
    [InlineData("v1")]
    [InlineData("v1.5")]
    public async Task ZaakInformatieObject_above_the_clients_maximum_vertrouwelijkheidaanduiding_is_forbidden(string version)
    {
        var id = await SeedZaakInformatieObjectAsync(CurrentTenant, ZaakTypeA, VertrouwelijkheidAanduiding.geheim);
        var client = Client(ReadScopes, Permission(ZaakTypeA, VertrouwelijkheidAanduiding.vertrouwelijk, AuthorizationScopes.Zaken.Read));

        var result = await GetAsync(version, id, client);

        Assert.Equal(QueryStatus.Forbidden, result.Status);
    }

    [Theory]
    [InlineData("v1")]
    [InlineData("v1.5")]
    public async Task Client_with_only_another_scope_on_the_zaaktype_is_forbidden(string version)
    {
        var id = await SeedZaakInformatieObjectAsync(CurrentTenant, ZaakTypeA);
        var client = Client(ReadScopes, Permission(ZaakTypeA, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Create));

        var result = await GetAsync(version, id, client);

        Assert.Equal(QueryStatus.Forbidden, result.Status);
    }

    [Theory]
    [InlineData("v1")]
    [InlineData("v1.5")]
    public async Task Patch_preload_with_only_update_scope_is_allowed(string version)
    {
        // The PATCH action first sends the GET query; it then runs under the PATCH action's requested scopes.
        var id = await SeedZaakInformatieObjectAsync(CurrentTenant, ZaakTypeA);
        var client = Client(PatchScopes, Permission(ZaakTypeA, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Update));

        var result = await GetAsync(version, id, client);

        Assert.Equal(QueryStatus.OK, result.Status);
    }

    [Theory]
    [InlineData("v1")]
    [InlineData("v1.5")]
    public async Task Client_with_all_authorizations_can_read_it(string version)
    {
        var id = await SeedZaakInformatieObjectAsync(CurrentTenant, ZaakTypeA, VertrouwelijkheidAanduiding.zeer_geheim);

        var result = await GetAsync(version, id, ClientWithAllAuthorizations(ReadScopes));

        Assert.Equal(QueryStatus.OK, result.Status);
    }

    [Theory]
    [InlineData("v1")]
    [InlineData("v1.5")]
    public async Task ZaakInformatieObject_of_another_tenant_is_not_found(string version)
    {
        var id = await SeedZaakInformatieObjectAsync(OtherTenant, ZaakTypeA);
        var client = Client(ReadScopes, Permission(ZaakTypeA, VertrouwelijkheidAanduiding.zeer_geheim, AuthorizationScopes.Zaken.Read));

        var result = await GetAsync(version, id, client);

        Assert.Equal(QueryStatus.NotFound, result.Status);
    }

    private async Task<Guid> SeedZaakInformatieObjectAsync(
        string owner,
        string zaakType,
        VertrouwelijkheidAanduiding vertrouwelijkheid = VertrouwelijkheidAanduiding.openbaar
    )
    {
        var zaak = NewZaak(owner, zaakType, vertrouwelijkheid);
        var zaakInformatieObject = new ZaakInformatieObject
        {
            Id = Guid.NewGuid(),
            Owner = owner,
            Zaak = zaak,
            InformatieObject = $"http://documenten.local/api/v1/enkelvoudiginformatieobjecten/{Guid.NewGuid()}",
            CreationTime = DateTime.UtcNow,
        };

        _context.Zaken.Add(zaak);
        _context.ZaakInformatieObjecten.Add(zaakInformatieObject);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        return zaakInformatieObject.Id;
    }

    private Task<QueryResult<ZaakInformatieObject>> GetAsync(string version, Guid id, IAuthorizationContextAccessor client) =>
        version switch
        {
            "v1" => new V1.GetZaakInformatieObjectQueryHandler(
                NullLogger<V1.GetZaakInformatieObjectQueryHandler>.Instance,
                CreateConfiguration(),
                Mock.Of<IEntityUriService>(),
                _context,
                client,
                KenmerkenResolver().Object
            ).Handle(new V1.GetZaakInformatieObjectQuery { Id = id }, CancellationToken.None),
            "v1.5" => new V1_5.GetZaakInformatieObjectQueryHandler(
                NullLogger<V1_5.GetZaakInformatieObjectQueryHandler>.Instance,
                CreateConfiguration(),
                Mock.Of<IEntityUriService>(),
                _context,
                client,
                KenmerkenResolver().Object
            ).Handle(new V1_5.GetZaakInformatieObjectQuery { Id = id }, CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(version)),
        };
}
