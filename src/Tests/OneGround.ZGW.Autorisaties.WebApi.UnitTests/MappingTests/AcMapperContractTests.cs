using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Mapping;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;
using ApplicatieRequestDtoV1 = OneGround.ZGW.Autorisaties.Contracts.v1.Requests.ApplicatieRequestDto;
using ApplicatieRequestDtoV11 = OneGround.ZGW.Autorisaties.Contracts.v1._1.Requests.ApplicatieRequestDto;

namespace OneGround.ZGW.Autorisaties.WebApi.UnitTests.MappingTests;

/// <summary>
/// Guards the mapping contracts AC depends on OUTSIDE its controllers' own <c>Map</c> calls: the PATCH
/// merge via <see cref="IZgwRequestMerger"/>, for both contract versions. The per-register tests in this
/// folder build an isolated TypeAdapterConfig and cannot see this path — they stayed green while the
/// v1 PATCH merge was resolving an AutoMapper map that had already been deleted.
/// </summary>
/// <remarks>
/// AC sets <c>RegisterSharedAudittrailHandlers = false</c> and has no expanders, so the merger is the
/// only such consumer today. Mapster convention-maps instead of throwing on a missing map, so a dropped
/// register here would produce a quietly wrong PATCH result rather than an exception.
/// </remarks>
public class AcMapperContractTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IZgwMapper _zgwMapper;
    private readonly IZgwRequestMerger _zgwRequestMerger;

    public AcMapperContractTests()
    {
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);

        // Mirrors Startup exactly: same extensions, same order, same assembly, EnableMapster on.
        services.AddAutoMapper(typeof(Startup).Assembly);
        services.AddZgwMapster(typeof(Startup).Assembly, enable: true);

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _zgwMapper = _scope.ServiceProvider.GetRequiredService<IZgwMapper>();
        _zgwRequestMerger = _scope.ServiceProvider.GetRequiredService<IZgwRequestMerger>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Fact]
    public void AC_resolves_the_Mapster_backed_mapper()
    {
        // If this ever regresses to the AutoMapper adapter, AC has no profiles left and every shared
        // consumer would map against an empty configuration — so assert the routing directly rather
        // than inferring it from a successful map.
        Assert.IsType<MapsterZgwMapper>(_zgwMapper);
    }

    [Fact]
    public void RequestMerger_can_merge_a_PATCH_onto_an_existing_Applicatie_v1()
    {
        var existing = ExistingApplicatie();
        var patch = new JObject { ["label"] = "gewijzigd label" };

        var merged = _zgwRequestMerger.MergePartialUpdateToObjectRequest<ApplicatieRequestDtoV1, Applicatie>(existing, patch);

        // The patched field comes from the JObject; the untouched fields can only come from the existing
        // entity having been mapped in first, which is the step that needs the register.
        Assert.Equal("gewijzigd label", merged.Label);
        Assert.Equal(existing.HeeftAlleAutorisaties, merged.HeeftAlleAutorisaties);
        Assert.Equal(["client-a"], merged.ClientIds);
    }

    [Fact]
    public void RequestMerger_can_merge_a_PATCH_onto_an_existing_Applicatie_v11()
    {
        var existing = ExistingApplicatie();
        var patch = new JObject { ["label"] = "gewijzigd label" };

        var merged = _zgwRequestMerger.MergePartialUpdateToObjectRequest<ApplicatieRequestDtoV11, Applicatie>(existing, patch);

        Assert.Equal("gewijzigd label", merged.Label);
        Assert.Equal(existing.HeeftAlleAutorisaties, merged.HeeftAlleAutorisaties);
        Assert.Equal(["client-a"], merged.ClientIds);
        // A PATCH that does not mention the v1.1-only field must not silently reset it.
        Assert.True(merged.AlleenIsGereedVoorPublicatie);
    }

    private static Applicatie ExistingApplicatie() =>
        new Applicatie
        {
            Id = Guid.NewGuid(),
            Label = "bestaand label",
            HeeftAlleAutorisaties = true,
            AlleenIsGereedVoorPublicatie = true,
            ClientIds = new List<ApplicatieClient> { new ApplicatieClient { ClientId = "client-a" } },
            Autorisaties = new List<Autorisatie>
            {
                new Autorisatie { Component = Component.zrc, Scopes = new[] { "zaken.lezen" } },
            },
        };
}
