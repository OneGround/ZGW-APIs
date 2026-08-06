using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Mapping;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.Contracts.v1.Requests;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Web;
using OneGround.ZGW.Notificaties.Web.Controllers.v1;
using Xunit;

namespace OneGround.ZGW.Notificaties.WebApi.UnitTests.MappingTests;

/// <summary>
/// Guards the mapping contract NRC depends on OUTSIDE its controllers: the PATCH merge via
/// <see cref="IZgwRequestMerger"/>. The per-register tests in this folder build an isolated
/// TypeAdapterConfig and cannot see that path — they passed while PATCH was broken at runtime.
/// </summary>
/// <remarks>
/// NRC has no audit trail (ApiServiceSettings.RegisterSharedAudittrailHandlers is false) and no
/// expanders, so unlike BRC the PATCH merge is the only out-of-controller mapping consumer.
/// <para>
/// Note the division of labour between the two merge-related facts here.
/// <see cref="RequestMerger_can_merge_a_PATCH_onto_an_existing_Abonnement"/> resolves
/// IZgwRequestMerger directly, so it proves the register still serves the merge but CANNOT detect a
/// controller wired to the AutoMapper-backed IRequestMerger — it passes either way.
/// <see cref="AbonnementController_depends_on_the_Mapster_backed_merger"/> is the fact that catches
/// that, and it is cheap because the controller and its constructor are public.
/// </para>
/// </remarks>
public class NrcMapperContractTests : IDisposable
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IZgwMapper _zgwMapper;
    private readonly IZgwRequestMerger _zgwRequestMerger;
    private readonly IMapper _mapsterMapper;

    public NrcMapperContractTests()
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
        _mapsterMapper = _scope.ServiceProvider.GetRequiredService<IMapper>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Fact]
    public void NRC_resolves_the_Mapster_backed_mapper()
    {
        // NRC consumes no IZgwMapper today, so this guards the routing rather than a live call path —
        // it becomes load-bearing the moment RegisterSharedAudittrailHandlers is turned on.
        Assert.IsType<MapsterZgwMapper>(_zgwMapper);
    }

    [Fact]
    public void RequestMerger_can_merge_a_PATCH_onto_an_existing_Abonnement()
    {
        var existing = _fixture.Create<Abonnement>();
        var patch = new JObject { ["callbackUrl"] = "https://example.test/new" };

        var merged = _zgwRequestMerger.MergePartialUpdateToObjectRequest<AbonnementRequestDto, Abonnement>(existing, patch);

        // The patched field comes from the JObject; the untouched fields can only come from the existing
        // entity having been mapped in first, which is the step that needs the register. Kanalen is the
        // load-bearing one — it cannot convention-map, because the source member is AbonnementKanalen.
        Assert.Equal("https://example.test/new", merged.CallbackUrl);
        Assert.Equal(existing.Auth, merged.Auth);
        Assert.Equal(existing.AbonnementKanalen.Count, merged.Kanalen.Count);
    }

    [Fact]
    public void AbonnementController_depends_on_the_Mapster_backed_merger()
    {
        // NRC has no AutoMapper maps left, so a PATCH routed through the AutoMapper-backed
        // IRequestMerger throws at runtime. There are no controller-level tests in this repo and the
        // MediatR queries are internal, so assert the dependency itself: this is the invariant that a
        // controller which PATCHes in a Mapster-only service must depend on the Mapster merger.
        var parameterTypes = typeof(AbonnementController).GetConstructors().Single().GetParameters().Select(p => p.ParameterType).ToArray();

        Assert.Contains(typeof(IZgwRequestMerger), parameterTypes);
    }

    [Fact]
    public void AbonnementRequestDto_with_a_kanaal_maps_to_Abonnement_without_crashing()
    {
        // Guards against an uncatchable StackOverflowException, not a regular exception. Must resolve
        // IMapper from the real AddZgwMapster-built provider (see constructor) - a hand-rolled
        // TypeAdapterConfig without its settings would stay green regardless of the register.
        var dto = new AbonnementRequestDto
        {
            CallbackUrl = "https://example.test/callback",
            Auth = "the-auth",
            Kanalen = new List<AbonnementKanaalDto>
            {
                new()
                {
                    Naam = "zaken",
                    Filters = new Dictionary<string, string> { ["resource"] = "zaakinformatieobject" },
                },
            },
        };

        var result = _mapsterMapper.Map<Abonnement>(dto);

        Assert.Single(result.AbonnementKanalen);
        Assert.Equal("zaken", result.AbonnementKanalen[0].Kanaal.Naam);
        Assert.Single(result.AbonnementKanalen[0].Filters);
        Assert.Equal("resource", result.AbonnementKanalen[0].Filters[0].Key);
    }
}
