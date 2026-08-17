using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Autorisaties.Contracts.v1.Requests;
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
/// The PATCH merge via <see cref="IZgwRequestMerger"/> — the one mapping contract AC depends on outside
/// its controllers. own Map calls. The per-register tests build an isolated config and cannot see it;
/// they stayed green while this path resolved an AutoMapper map that had already been deleted.
/// </summary>
/// <remarks>
/// A dropped register here is silent: Mapster convention-maps instead of throwing, so the PATCH result
/// comes back quietly wrong rather than as an exception.
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
        // AC has no AutoMapper profiles left, so a regression to that adapter would map every shared
        // consumer against an empty configuration. Asserted directly, not inferred from a working map.
        Assert.IsType<MapsterZgwMapper>(_zgwMapper);
    }

    [Fact]
    public void RequestMerger_can_merge_a_PATCH_onto_an_existing_Applicatie_v1()
    {
        var existing = ExistingApplicatie();
        var patch = new JObject { ["label"] = "gewijzigd label" };

        var merged = _zgwRequestMerger.MergePartialUpdateToObjectRequest<ApplicatieRequestDtoV1, Applicatie>(existing, patch);

        // The untouched fields can only come from the existing entity having been mapped in first —
        // the step that needs the register.
        Assert.Equal("gewijzigd label", merged.Label);
        Assert.Equal(existing.HeeftAlleAutorisaties, merged.HeeftAlleAutorisaties);
        Assert.Equal(["client-a"], merged.ClientIds);
        AssertAutorisatiesSurvivedTheMerge(merged.Autorisaties);
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
        // v1.1 reuses v1's AUTORISATIE request DTO, so this resolves through v1's register.
        AssertAutorisatiesSurvivedTheMerge(merged.Autorisaties);
    }

    /// <summary>
    /// A PATCH that never mentions autorisaties still round-trips them through the request DTO. If that
    /// nested mapping is dropped they come back empty and the update handler removes every authorization
    /// the application had, answering 200.
    /// </summary>
    private static void AssertAutorisatiesSurvivedTheMerge(List<AutorisatieRequestDto> autorisaties)
    {
        var autorisatie = Assert.Single(autorisaties);
        Assert.Equal(Component.zrc.ToString(), autorisatie.Component);
        Assert.Equal(["zaken.lezen"], autorisatie.Scopes);
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
