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
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Mapping;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;
using ZaakTypeRequestDtoV1 = OneGround.ZGW.Catalogi.Contracts.v1.Requests.ZaakTypeRequestDto;
using ZaakTypeRequestDtoV13 = OneGround.ZGW.Catalogi.Contracts.v1._3.Requests.ZaakTypeRequestDto;
using ZaakTypeResponseDtoV1 = OneGround.ZGW.Catalogi.Contracts.v1.Responses.ZaakTypeResponseDto;
using ZaakTypeResponseDtoV13 = OneGround.ZGW.Catalogi.Contracts.v1._3.Responses.ZaakTypeResponseDto;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests;

/// <summary>
/// The two mapping contracts ZTC depends on outside the Map calls its controllers make themselves: the
/// audit trail (<see cref="IZgwMapper"/>) and the PATCH merge (<see cref="IZgwRequestMerger"/>). The
/// register tests resolve <c>MapsterMapper.IMapper</c> directly and so exercise neither adapter.
/// </summary>
/// <remarks>
/// ZTC's AutoMapper profiles are deleted, so both consumers must resolve the Mapster-backed adapters.
/// A regression here is silent, not loud: Mapster convention-maps instead of throwing, so an audit
/// record or a PATCH result comes back quietly wrong rather than as an exception. That is why the
/// adapter type is asserted directly rather than inferred from a working map, and why the controller
/// constructors are checked by reflection - a controller left on AutoMapper's
/// <see cref="IRequestMerger"/> keeps compiling and every mapping fact below keeps passing, because
/// they resolve the correct merger themselves.
/// </remarks>
public class ZtcMapperContractTests : IDisposable
{
    private static readonly Guid GerelateerdZaakTypeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DeelZaakTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IZgwMapper _zgwMapper;
    private readonly IZgwRequestMerger _zgwRequestMerger;

    public ZtcMapperContractTests()
    {
        // Prefixing, never echoing: the real UriService.GetUri returns an ABSOLUTE url while
        // entity.Url is a relative path, and a mock that echoes e.Url collapses that difference --
        // Mapster's convention copy of the same-named Url member then satisfies every URL assertion
        // below on its own. This class asserted `existing.Url == dto.Url` against an echoing mock, which
        // is a false positive the sibling register tests had already been corrected for. Shares
        // ZtcMapperTestHost's prefix so there is one definition of "resolved" across the suite.
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(ZtcMapperTestHost.Resolved);

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
    public void ZTC_resolves_the_Mapster_backed_mapper()
    {
        // ZTC has no AutoMapper profiles left, so a regression to that adapter would map every shared
        // consumer against an empty configuration.
        Assert.IsType<MapsterZgwMapper>(_zgwMapper);
    }

    [Fact]
    public void AuditTrail_maps_an_existing_ZaakType_to_its_v1_response_dto()
    {
        // AuditTrailServiceBase.SetOld/SetNew go through IZgwMapper with exactly these DTOs, and every
        // mutating ZTC endpoint writes an audit record that way, so a dropped register turns every
        // POST/PUT/PATCH/DELETE audit entry into a convention-mapped approximation.
        var existing = ExistingZaakType();

        var dto = _zgwMapper.Map<ZaakTypeResponseDtoV1>(existing);

        Assert.Equal(ZtcMapperTestHost.Resolved(existing), dto.Url);
        Assert.Equal("ZAAKTYPE-001", dto.Identificatie);
        Assert.Equal(ZtcMapperTestHost.Resolved(existing.Catalogus), dto.Catalogus);
        // The reliable discriminator is a type or format mismatch, not a name difference: DateOnly on
        // the entity, string on the DTO. Convention copy cannot satisfy it at all, so unlike a
        // same-name string member it fails the moment the register's StringDateFromDate rule goes
        // missing.
        Assert.Equal("2026-01-01", dto.BeginGeldigheid);
        AssertGerelateerdeZaakTypenSurvived(dto.GerelateerdeZaakTypen.Select(g => (g.AardRelatie, g.Toelichting, g.ZaakType)));
    }

    [Fact]
    public void AuditTrail_maps_an_existing_ZaakType_to_its_v1_3_response_dto()
    {
        var existing = ExistingZaakType();

        var dto = _zgwMapper.Map<ZaakTypeResponseDtoV13>(existing);

        Assert.Equal(ZtcMapperTestHost.Resolved(existing), dto.Url);
        Assert.Equal("ZAAKTYPE-001", dto.Identificatie);
        Assert.Equal(ZtcMapperTestHost.Resolved(existing.Catalogus), dto.Catalogus);
        Assert.Equal("2026-01-01", dto.BeginGeldigheid);
        AssertGerelateerdeZaakTypenSurvived(dto.GerelateerdeZaakTypen.Select(g => (g.AardRelatie, g.Toelichting, g.ZaakType)));
    }

    [Fact]
    public void RequestMerger_can_merge_a_PATCH_onto_an_existing_ZaakType_v1()
    {
        var existing = ExistingZaakType();
        var patch = new JObject { ["omschrijving"] = "gewijzigde omschrijving" };

        var merged = _zgwRequestMerger.MergePartialUpdateToObjectRequest<ZaakTypeRequestDtoV1, ZaakType>(existing, patch);

        // The untouched fields can only come from the existing entity having been mapped in first -
        // the step that needs the register.
        Assert.Equal("gewijzigde omschrijving", merged.Omschrijving);
        Assert.Equal("ZAAKTYPE-001", merged.Identificatie);
        Assert.Equal(ZtcMapperTestHost.Resolved(existing.Catalogus), merged.Catalogus);
        Assert.Equal("2026-01-01", merged.BeginGeldigheid);
        AssertGerelateerdeZaakTypenSurvived(merged.GerelateerdeZaakTypen.Select(g => (g.AardRelatie, g.Toelichting, g.ZaakType)));
        AssertRelationUrlsSurvived(merged.DeelZaakTypen, merged.BesluitTypen, existing);
    }

    [Fact]
    public void RequestMerger_can_merge_a_PATCH_onto_an_existing_ZaakType_v1_3()
    {
        var existing = ExistingZaakType();
        var patch = new JObject { ["omschrijving"] = "gewijzigde omschrijving" };

        var merged = _zgwRequestMerger.MergePartialUpdateToObjectRequest<ZaakTypeRequestDtoV13, ZaakType>(existing, patch);

        Assert.Equal("gewijzigde omschrijving", merged.Omschrijving);
        Assert.Equal("ZAAKTYPE-001", merged.Identificatie);
        Assert.Equal(ZtcMapperTestHost.Resolved(existing.Catalogus), merged.Catalogus);
        Assert.Equal("2026-01-01", merged.BeginGeldigheid);
        // A PATCH that does not mention a v1.3-only field must not silently reset it.
        Assert.Equal("Team Vergunningen", merged.Verantwoordelijke);

        // v1.3 identifies related types by their denormalized identificatie/omschrijving rather than by
        // URL (the v1 request map and both response maps use URLs) - asserted on the values the v1
        // facts deliberately do not use, so the two registers cannot satisfy each other's expectations.
        var relation = Assert.Single(merged.GerelateerdeZaakTypen);
        Assert.Equal(AardRelatie.vervolg.ToString(), relation.AardRelatie);
        Assert.Equal("volgt op de aanvraag", relation.Toelichting);
        Assert.Equal("ZAAKTYPE-GERELATEERD", relation.ZaakType);
        Assert.Equal(["ZAAKTYPE-DEEL"], merged.DeelZaakTypen);
        Assert.Equal(["besluittype-omschrijving"], merged.BesluitTypen);
    }

    /// <summary>
    /// Every entity → response-DTO pair the registers declare, mapped through <see cref="IZgwMapper"/> —
    /// the adapter <c>AuditTrailServiceBase.SetOld</c>/<c>SetNew</c> uses. The two ZaakType facts above
    /// cover one DTO name out of the ten ZTC hands the audit trail across two API versions from 61
    /// files; this covers all of them, and is discovered from the real <c>AddZgwMapster</c> config so no
    /// list goes stale when a version gains a type.
    /// </summary>
    /// <remarks>
    /// The assertion is that the URL is ABSOLUTE, which is what gives it teeth: every one of these DTOs
    /// has a same-named <c>Url</c> string that Mapster convention-copies from the entity's own RELATIVE
    /// <c>Url</c>, so <c>dto.Url == entity.Url</c> passes without the register's
    /// <c>MapsterUrlResolver.ResolveUrl</c> rule ever running. Comparing against the mock's prefixed
    /// value fails the moment that rule is dropped — for every pair at once, not just ZaakType.
    /// <para>
    /// The source entity is built bare (<c>Id</c> plus empty collections) rather than by AutoFixture.
    /// Empty collections are required because several registers' <c>.AfterMapping</c> folds iterate a
    /// navigation with no null guard — deliberately, matching the AutoMapper originals — and a bare
    /// entity is deterministic, so unlike a fixture-driven fact this cannot be defeated by generated
    /// data that happens to miss a path.
    /// </para>
    /// <para>
    /// Mutation check: delete any one <c>.Map(dest =&gt; dest.Url, ...)</c> rule and this fails for that
    /// pair, with no other fact in the suite noticing — which is the coverage it adds.
    /// </para>
    /// </remarks>
    [Theory]
    [MemberData(nameof(EntityToResponseDtoPairs))]
    public void AuditTrail_resolves_an_absolute_url_for_every_declared_response_dto(Type entityType, Type responseDtoType)
    {
        var entity = (IUrlEntity)BareEntity(entityType);

        var dto = MapThroughZgwMapper(responseDtoType, entity);

        Assert.Equal(ZtcMapperTestHost.Resolved(entity), (string)responseDtoType.GetProperty("Url")!.GetValue(dto));
    }

    /// <summary>
    /// Every entity → request-DTO pair the registers declare, run through the real
    /// <see cref="IZgwRequestMerger"/> with an empty patch. A routing tripwire, not a value check: the
    /// values are pinned by the register tests, whereas the failure this catches is the one that shipped
    /// on 17 controllers before commit d288c53 — a merge resolved against a mapper that has no map for
    /// the pair, which throws at request time while every register-level fact stays green.
    /// </summary>
    /// <remarks>
    /// Deliberately does not assert on mapped values. With a bare entity every navigation is null, so
    /// the url-shaped members are legitimately null and there is nothing for a value assertion to
    /// discriminate; inventing one here would be decorative. What it does prove for all pairs at once is
    /// that <c>IZgwRequestMerger</c> resolves, that the pair has a real map behind it, and that the
    /// <c>.AfterMapping</c> folds survive an entity with nothing loaded.
    /// </remarks>
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
    /// Reads the pairs out of the config <c>AddZgwMapster</c> actually builds. A hand-rolled
    /// <c>TypeAdapterConfig</c> would not do — it is the scanned, merged config that decides which
    /// definition of a pair survives, and that is the one the service resolves.
    /// </summary>
    private static TheoryData<Type, Type> DeclaredPairsEndingIn(string destinationSuffix, bool requireUrlOnDestination)
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(Startup).Assembly, enable: true);
        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<TypeAdapterConfig>();

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

        // Guards the discovery itself: a filter that matched nothing would make the facts above vacuous
        // rather than failing. ZTC declares 10 response pairs and 8 request pairs per API version.
        Assert.NotEmpty(data);

        return data;
    }

    /// <summary>
    /// An entity with only <c>Id</c> set and every writable collection navigation initialised empty.
    /// The empty collections are load-bearing, not tidiness: the <c>GerelateerdeZaakTypen</c>
    /// <c>.AfterMapping</c> folds iterate their navigation unguarded (as the AutoMapper
    /// <c>IMappingAction</c>s they replace did), so a null there is a NullReferenceException.
    /// </summary>
    private static object BareEntity(Type entityType)
    {
        var entity = Activator.CreateInstance(entityType);

        foreach (var property in entityType.GetProperties().Where(p => p.CanWrite && p.CanRead))
        {
            if (property.Name == nameof(IBaseEntity.Id) && property.PropertyType == typeof(Guid))
            {
                property.SetValue(entity, Guid.NewGuid());
                continue;
            }

            if (!property.PropertyType.IsGenericType)
            {
                continue;
            }

            var definition = property.PropertyType.GetGenericTypeDefinition();
            if (definition == typeof(List<>) || definition == typeof(ICollection<>) || definition == typeof(IList<>))
            {
                property.SetValue(entity, Activator.CreateInstance(typeof(List<>).MakeGenericType(property.PropertyType.GetGenericArguments()[0])));
            }
        }

        return entity;
    }

    private object MapThroughZgwMapper(Type destinationType, object source) =>
        typeof(IZgwMapper).GetMethod(nameof(IZgwMapper.Map))!.MakeGenericMethod(destinationType).Invoke(_zgwMapper, [source]);

    private object MergeEmptyPatch(Type requestDtoType, Type entityType, object entity) =>
        typeof(IZgwRequestMerger)
            .GetMethod(nameof(IZgwRequestMerger.MergePartialUpdateToObjectRequest))!
            .MakeGenericMethod(requestDtoType, entityType)
            .Invoke(_zgwRequestMerger, [entity, new JObject()]);

    /// <summary>
    /// Every ZTC controller that runs a PATCH must take <see cref="IZgwRequestMerger"/>, not only the
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
    /// Nothing in ZTC may CALL AutoMapper any more. The constructor fact above only proves the Mapster
    /// merger is injected; every controller still takes <c>AutoMapper.IMapper</c> and
    /// <see cref="IRequestMerger"/> because the shared <c>ZGWControllerBase</c> demands them, and
    /// <c>ZGWControllerBase._mapper</c> stays visible to every ZTC controller as a protected field.
    /// </summary>
    /// <remarks>
    /// Since ZTC's profiles are deleted, that field is a mapper over an EMPTY AutoMapper configuration.
    /// A merge from a branch cut before the migration, or a paste from a service that has not migrated,
    /// can reintroduce <c>_mapper.Map&lt;ZaakTypeResponseDto&gt;(...)</c> or
    /// <c>_requestMerger.MergePartialUpdateToObjectRequest(...)</c>: it compiles, no mapping fact
    /// notices (they resolve their own mapper), and it throws only when a real request hits that action.
    /// <para>
    /// Detected by reading the compiled assembly's MemberRef table rather than by walking IL: a call
    /// into AutoMapper emits a MemberRef whose parent is an AutoMapper TypeRef, while merely having
    /// <c>AutoMapper.IMapper</c> as a constructor parameter does not (that is a TypeRef in a signature,
    /// and the base-constructor call's MemberRef is parented to ZGWControllerBase). So this fires on
    /// use, never on the injection the base class forces.
    /// </para>
    /// <para>
    /// Scope is the <c>Catalogi.Web</c> assembly — every ZTC controller, handler and register lives
    /// there, and the <c>Catalogi.WebApi</c> host has no AutoMapper reference at all (verified). Add the
    /// host assembly here if that ever stops being true.
    /// </para>
    /// <para>
    /// Mutation check: switch one <c>_mapsterMapper.Map&lt;T&gt;</c> call back to <c>_mapper.Map&lt;T&gt;</c>
    /// and this fails; the constructor parameter every controller still declares does not trigger it.
    /// </para>
    /// <para>Delete this fact once <c>ZGWControllerBase</c> itself drops its AutoMapper dependency.</para>
    /// </remarks>
    [Fact]
    public void No_ZTC_code_calls_AutoMapper_or_the_AutoMapper_backed_request_merger()
    {
        var assemblyPath = typeof(Startup).Assembly.Location;

        // A single-file or in-memory host reports an empty Location; fail with that reason rather than an
        // opaque IO error that reads like the assertion below found nothing.
        Assert.False(string.IsNullOrEmpty(assemblyPath), "Cannot scan metadata: Catalogi.Web has no on-disk location.");

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();

        var calls = new List<string>();
        foreach (var handle in metadata.MemberReferences)
        {
            var memberReference = metadata.GetMemberReference(handle);
            if (memberReference.Parent.Kind != HandleKind.TypeReference)
            {
                continue;
            }

            var typeReference = metadata.GetTypeReference((TypeReferenceHandle)memberReference.Parent);
            var declaringNamespace = metadata.GetString(typeReference.Namespace);
            var declaringType = metadata.GetString(typeReference.Name);

            var isAutoMapper = declaringNamespace == "AutoMapper" || declaringNamespace.StartsWith("AutoMapper.", StringComparison.Ordinal);
            var isAutoMapperMerger = declaringType == nameof(IRequestMerger);
            if (isAutoMapper || isAutoMapperMerger)
            {
                calls.Add($"{declaringNamespace}.{declaringType}.{metadata.GetString(memberReference.Name)}");
            }
        }

        Assert.True(
            calls.Count == 0,
            "ZTC has no AutoMapper profiles left, so these calls run against an empty configuration and "
                + "throw at request time. Use MapsterMapper.IMapper / IZgwRequestMerger instead:\n  "
                + string.Join("\n  ", calls.Distinct().OrderBy(c => c))
        );
    }

    /// <summary>
    /// Discovered rather than listed, so a controller that gains a PATCH later is covered without
    /// anyone remembering to extend a list. Which merger field a method body uses is not visible by
    /// reflection, so "patching" is taken from the action name ZTC controllers use for it.
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

    /// <summary>
    /// This collection is produced by an <c>.AfterMapping</c> block rather than by convention, so it is
    /// the first thing to disappear if the register is dropped - and it comes back as an empty list
    /// rather than an error, which on a PATCH means the update handler removes every relation the
    /// ZAAKTYPE had and answers 200.
    /// </summary>
    private static void AssertGerelateerdeZaakTypenSurvived(
        IEnumerable<(string AardRelatie, string Toelichting, string ZaakType)> gerelateerdeZaakTypen
    )
    {
        var item = Assert.Single(gerelateerdeZaakTypen);
        Assert.Equal(AardRelatie.vervolg.ToString(), item.AardRelatie);
        Assert.Equal("volgt op de aanvraag", item.Toelichting);
        Assert.Equal($"{ZtcMapperTestHost.BaseUrl}/zaaktypen/{GerelateerdZaakTypeId}", item.ZaakType);
    }

    private static void AssertRelationUrlsSurvived(IEnumerable<string> deelZaakTypen, IEnumerable<string> besluitTypen, ZaakType existing)
    {
        Assert.Equal([$"{ZtcMapperTestHost.BaseUrl}/zaaktypen/{DeelZaakTypeId}"], deelZaakTypen);
        Assert.Equal([ZtcMapperTestHost.Resolved(existing.ZaakTypeBesluitTypen.Single().BesluitType)], besluitTypen);
    }

    private static ZaakType ExistingZaakType() =>
        new ZaakType
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Identificatie = "ZAAKTYPE-001",
            Omschrijving = "bestaande omschrijving",
            Verantwoordelijke = "Team Vergunningen",
            BeginGeldigheid = new DateOnly(2026, 1, 1),
            VersieDatum = new DateOnly(2026, 1, 1),
            Catalogus = new Catalogus { Id = Guid.Parse("44444444-4444-4444-4444-444444444444") },
            ZaakTypeGerelateerdeZaakTypen =
            [
                new ZaakTypeGerelateerdeZaakType
                {
                    AardRelatie = AardRelatie.vervolg,
                    Toelichting = "volgt op de aanvraag",
                    GerelateerdeZaakType = new ZaakType { Id = GerelateerdZaakTypeId },
                    GerelateerdeZaakTypeIdentificatie = "ZAAKTYPE-GERELATEERD",
                },
            ],
            ZaakTypeDeelZaakTypen =
            [
                new ZaakTypeDeelZaakType
                {
                    DeelZaakType = new ZaakType { Id = DeelZaakTypeId },
                    DeelZaakTypeIdentificatie = "ZAAKTYPE-DEEL",
                },
            ],
            ZaakTypeBesluitTypen =
            [
                new ZaakTypeBesluitType
                {
                    BesluitType = new BesluitType { Id = Guid.Parse("55555555-5555-5555-5555-555555555555") },
                    BesluitTypeOmschrijving = "besluittype-omschrijving",
                },
            ],
        };
}
