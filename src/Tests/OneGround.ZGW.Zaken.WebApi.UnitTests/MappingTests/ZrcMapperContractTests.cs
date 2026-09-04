using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Mapping;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.Web;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

/// <summary>
/// The two mapping contracts ZRC depends on outside the Map calls its controllers make themselves: the
/// audit trail (<see cref="IZgwMapper"/>) and the PATCH merge (<see cref="IZgwRequestMerger"/>). The
/// register tests resolve <c>MapsterMapper.IMapper</c> directly and so exercise neither adapter.
/// </summary>
/// <remarks>
/// A regression here is silent, not loud: Mapster convention-maps instead of throwing, so an audit
/// record or a PATCH result comes back quietly wrong. Hence the adapter type is asserted directly rather
/// than inferred from a working map, and the controller constructors are checked by reflection — a
/// controller left on the AutoMapper merger keeps compiling and every mapping fact here keeps passing,
/// because they resolve the correct merger themselves.
/// </remarks>
public class ZrcMapperContractTests : IDisposable
{
    /// <summary>
    /// Entity string members a mapping body hands straight to a JSON parser. Their columns are NOT NULL,
    /// so a bare instance leaving them unset is an artifact of this file, not a state a persisted entity
    /// can be in — and because Mapster does not null-guard a member used inside a method call, an unset
    /// one surfaces as a parser <c>ArgumentNullException</c> rather than as a mapping failure. Seeded with
    /// the smallest valid JSON document so the theories below assert about mapping instead.
    /// <para>
    /// Not merely a quirk of this file: AutoMapper DID guard that shape (it short-circuited the whole
    /// <c>MapFrom</c> expression to default when any source member in it was null, so the parser was never
    /// called), so this is a real divergence and the NOT NULL column — not the mapper — is what makes the
    /// ported bodies safe. The seam pins it in
    /// <c>MapsterSeamHealthTests.A_source_member_passed_as_a_method_ARGUMENT_is_not_null_guarded</c> and its
    /// receiver-shaped twin; read those before adding a ternary to any register.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> JsonValuedMembers = [nameof(OverigeZaakObject.OverigeData)];

    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IZgwMapper _zgwMapper;
    private readonly IZgwRequestMerger _zgwRequestMerger;

    public ZrcMapperContractTests()
    {
        // Prefixing, never echoing -- see ZrcMapperTestHost.BaseUrl. Shares that prefix so there is one
        // definition of "resolved" across the suite.
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(ZrcMapperTestHost.Resolved);

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);

        // Mirrors Startup exactly: same extensions, same order, same assembly. The order
        // matters - the seam uses services.Replace for IZgwMapper, and Replace on an empty collection
        // merely adds, which would leave the type assertion below green without proving the replace wins.
        services.AddAutoMapper(typeof(Startup).Assembly);
        services.AddZgwMapster(typeof(Startup).Assembly);

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
    public void ZRC_resolves_the_Mapster_backed_mapper()
    {
        // ZRC has no AutoMapper profiles left, so a regression to that adapter would map every shared
        // consumer against an empty configuration.
        Assert.IsType<MapsterZgwMapper>(_zgwMapper);
    }

    /// <summary>
    /// Every entity → response-DTO pair the registers declare, mapped through the adapter
    /// <c>AuditTrailServiceBase.SetOld</c>/<c>SetNew</c> uses. Asserting the URL is ABSOLUTE is what
    /// gives it teeth: these DTOs all have a same-named <c>Url</c> that Mapster convention-copies from
    /// the entity's relative one, so a pair whose resolver rule is dropped yields the relative value and
    /// fails here.
    /// </summary>
    /// <remarks>
    /// The prefix rather than the exact resolved url is asserted deliberately. A theory over discovered
    /// pairs cannot know which entity a given pair resolves - a map may legitimately resolve a RELATED
    /// entity's url rather than its own, and one register maps <c>dest.Url</c> straight from
    /// <c>src.Url</c> as a domain data field rather than as a self-link (that pair's destination does not
    /// end in <c>ResponseDto</c> today, so the filter below excludes it, but the prefix form stays
    /// correct if it ever stops doing so).
    /// <para>
    /// Observed failure mode: deleting one pair's resolver rule — the
    /// <c>.Map(dest =&gt; dest.Url, src =&gt; MapsterUrlResolver.ResolveUrl(src))</c> on
    /// <c>ZaakEigenschap -&gt; ZaakEigenschapResponseDto</c> — fails that theory case and only that one, with
    /// <c>Assert.StartsWith() Failure: String start does not match</c>, <c>String: "/zaken/&lt;guid&gt;"</c>,
    /// <c>Expected start: "https://zrc.test"</c>. The relative value is the entity's own <c>Url</c>, i.e.
    /// Mapster's same-name convention copy — the silent substitution this theory exists to catch, and the
    /// reason the assertion is on the ABSOLUTE form rather than on the url being non-empty.
    /// </para>
    /// </remarks>
    [Theory]
    [MemberData(nameof(EntityToResponseDtoPairs))]
    public void AuditTrail_resolves_an_absolute_url_for_every_declared_response_dto(Type entityType, Type responseDtoType)
    {
        var entity = (IUrlEntity)BareEntity(entityType);

        var dto = MapThroughZgwMapper(responseDtoType, entity);

        var url = (string)responseDtoType.GetProperty("Url")!.GetValue(dto);

        Assert.StartsWith(ZrcMapperTestHost.BaseUrl, url);
    }

    /// <summary>
    /// Every entity → request-DTO pair the registers declare, run through the real
    /// <see cref="IZgwRequestMerger"/> with an empty patch. A routing tripwire, not a value check —
    /// values are pinned by the register tests; this catches a merge resolved against a mapper that has
    /// no map for the pair, which throws at request time while every register-level fact stays green.
    /// </summary>
    [Theory]
    [MemberData(nameof(EntityToRequestDtoPairs))]
    public void RequestMerger_merges_an_empty_patch_for_every_declared_request_dto(Type entityType, Type requestDtoType)
    {
        var entity = BareEntity(entityType);

        var merged = MergeEmptyPatch(requestDtoType, entityType, entity);

        Assert.NotNull(merged);
        Assert.IsType(requestDtoType, merged);
    }

    public static TheoryData<Type, Type> EntityToResponseDtoPairs() => DeclaredPairsEndingIn("ResponseDto", requireUrlOnDestination: true);

    public static TheoryData<Type, Type> EntityToRequestDtoPairs() => DeclaredPairsEndingIn("RequestDto", requireUrlOnDestination: false);

    /// <summary>
    /// The config <c>AddZgwMapster</c> actually builds — the scanned, merged one that decides which
    /// definition of a pair survives. Built once: both <c>MemberData</c> sources below read it, and a
    /// scan-plus-merge of the whole Web assembly is the slowest part of this class. Read-only here (only
    /// <c>RuleMap</c> keys), so sharing it cannot leak state between the two — unlike
    /// <c>ZrcMapsterCompileTests</c>, whose facts each mutate <c>Default</c> and so need their own.
    /// </summary>
    private static readonly Lazy<TypeAdapterConfig> DeclaredConfig = new(() =>
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(Startup).Assembly);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<TypeAdapterConfig>();
    });

    /// <summary>
    /// Reads the pairs out of the config <c>AddZgwMapster</c> actually builds — the scanned, merged one
    /// that decides which definition of a pair survives.
    /// </summary>
    private static TheoryData<Type, Type> DeclaredPairsEndingIn(string destinationSuffix, bool requireUrlOnDestination)
    {
        var config = DeclaredConfig.Value;

        var data = new TheoryData<Type, Type>();
        var pairs = config
            .RuleMap.Keys.Where(k =>
                typeof(IBaseEntity).IsAssignableFrom(k.Source)
                && k.Destination.Name.EndsWith(destinationSuffix, StringComparison.Ordinal)
                && (!requireUrlOnDestination || (typeof(IUrlEntity).IsAssignableFrom(k.Source) && k.Destination.GetProperty("Url") != null))
            )
            .OrderBy(k => k.Source.FullName)
            .ThenBy(k => k.Destination.FullName);

        foreach (var pair in pairs)
        {
            data.Add(pair.Source, pair.Destination);
        }

        // A filter that matched nothing would make the facts above vacuous rather than failing.
        Assert.NotEmpty(data);

        return data;
    }

    /// <summary>
    /// An entity with only <c>Id</c> set, every writable collection navigation initialised empty and
    /// every writable entity-typed reference navigation filled with one such bare instance. Neither is
    /// defensive: the ported mapping bodies read collection navigations and walk one reference hop
    /// (<c>src.Zaak.ZaakStatussen</c>) unguarded, so a null there is a NullReferenceException rather than
    /// a clean failure. Nesting stops after that single hop, which is all any register needs, and which
    /// keeps a self-referencing navigation from recursing. See <see cref="JsonValuedMembers"/> for the
    /// one member class that additionally needs a value rather than a shape.
    /// </summary>
    private static object BareEntity(Type entityType, bool fillReferences = true)
    {
        var entity = Activator.CreateInstance(entityType);

        foreach (var property in entityType.GetProperties().Where(p => p.CanWrite && p.CanRead))
        {
            if (property.Name == nameof(IBaseEntity.Id) && property.PropertyType == typeof(Guid))
            {
                property.SetValue(entity, Guid.NewGuid());
                continue;
            }

            if (property.PropertyType == typeof(string) && JsonValuedMembers.Contains(property.Name))
            {
                property.SetValue(entity, "{}");
                continue;
            }

            if (property.PropertyType.IsGenericType)
            {
                var definition = property.PropertyType.GetGenericTypeDefinition();
                if (definition == typeof(List<>) || definition == typeof(ICollection<>) || definition == typeof(IList<>))
                {
                    property.SetValue(
                        entity,
                        Activator.CreateInstance(typeof(List<>).MakeGenericType(property.PropertyType.GetGenericArguments()[0]))
                    );
                }

                continue;
            }

            if (fillReferences && typeof(IBaseEntity).IsAssignableFrom(property.PropertyType) && !property.PropertyType.IsAbstract)
            {
                property.SetValue(entity, BareEntity(property.PropertyType, fillReferences: false));
            }
        }

        return entity;
    }

    private object MapThroughZgwMapper(Type destinationType, object source) =>
        typeof(IZgwMapper).GetMethod(nameof(IZgwMapper.Map))!.MakeGenericMethod(destinationType).Invoke(_zgwMapper, [source]);

    // MergePartialUpdateToObjectRequest's afterMap parameter is optional, but MethodBase.Invoke does not
    // apply optional-parameter defaults - the argument array has to carry it explicitly.
    private object MergeEmptyPatch(Type requestDtoType, Type entityType, object entity) =>
        typeof(IZgwRequestMerger)
            .GetMethod(nameof(IZgwRequestMerger.MergePartialUpdateToObjectRequest))!
            .MakeGenericMethod(requestDtoType, entityType)
            .Invoke(_zgwRequestMerger, [entity, new JObject(), null]);

    /// <summary>
    /// Every ZRC controller that runs a PATCH must take <see cref="IZgwRequestMerger"/>, not only the
    /// AutoMapper <see cref="IRequestMerger"/> that <c>ZGWControllerBase</c> still requires. Asserted
    /// structurally because it cannot be observed from a mapping test: the merge facts above resolve
    /// the Mapster merger themselves and stay green while a controller merges through the AutoMapper
    /// one against an empty configuration.
    /// </summary>
    [Theory]
    [MemberData(nameof(PatchingControllerTypes))]
    public void Every_patching_controller_takes_the_Mapster_request_merger(Type controllerType)
    {
        var parameterTypes = controllerType.GetConstructors().Single().GetParameters().Select(p => p.ParameterType);

        Assert.Contains(typeof(IZgwRequestMerger), parameterTypes);
    }

    /// <summary>
    /// Discovered rather than listed, so a controller that gains a PATCH later is covered without
    /// anyone remembering to extend a list. Which merger field a method body uses is not visible by
    /// reflection, so "patching" is taken from the action name ZRC controllers use for it.
    /// </summary>
    public static TheoryData<Type> PatchingControllerTypes()
    {
        var controllers = typeof(Startup)
            .Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller") && t.GetMethod("PartialUpdateAsync") != null)
            .OrderBy(t => t.FullName);

        var data = new TheoryData<Type>();
        foreach (var controller in controllers)
        {
            data.Add(controller);
        }

        // Guards the discovery itself: a filter that silently matched nothing would make the fact
        // above vacuous rather than failing.
        Assert.NotEmpty(data);

        return data;
    }
}
