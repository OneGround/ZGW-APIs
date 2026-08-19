using System;
using System.Collections.Generic;
using System.Linq;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Autorisaties.Contracts.v1.Requests;
using OneGround.ZGW.Autorisaties.Contracts.v1.Responses;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Autorisaties.WebApi.UnitTests.MappingTests;

public class AcMapsterWiringTests
{
    [Fact]
    public void AddZgwMapster_discovers_AC_registers_from_the_web_assembly()
    {
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns("https://example.test/resolved-via-di");

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);
        // enable: true is load-bearing — without it the seam registers nothing at all.
        services.AddZgwMapster(typeof(DomainToResponseRegister).Assembly, enable: true);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var applicatie = new Applicatie
        {
            Id = Guid.NewGuid(),
            Label = "test",
            ClientIds = new List<ApplicatieClient>(),
        };

        var result = mapper.Map<ApplicatieResponseDto>(applicatie);

        // Compared against the mock.s literal, not applicatie.Url: the two Url members share a name and
        // type, so an echo assertion would pass even with the resolver unwired.
        Assert.Equal("https://example.test/resolved-via-di", result.Url);
        mockedUriService.Verify(s => s.GetUri(It.IsAny<IUrlEntity>()), Times.AtLeastOnce());

        // Autorisaties is left null above; the seam.s EmptyCollectionIfNull transform must make it empty.
        Assert.NotNull(result.Autorisaties);
        Assert.Empty(result.Autorisaties);
    }

    [Fact]
    public void AddZgwMapster_discovers_the_v1_1_registers_too()
    {
        // v1.1.s registers sit in a nested namespace but the same assembly — a version folder is not a
        // scan boundary.
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns("https://example.test/resolved-via-di");

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);
        services.AddZgwMapster(typeof(DomainToResponseRegister).Assembly, enable: true);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var applicatie = new Applicatie
        {
            Id = Guid.NewGuid(),
            Label = "test",
            AlleenIsGereedVoorPublicatie = true,
            ClientIds = new List<ApplicatieClient>(),
        };

        var result = mapper.Map<Contracts.v1._1.Responses.ApplicatieResponseDto>(applicatie);

        Assert.Equal("https://example.test/resolved-via-di", result.Url);
        Assert.True(result.AlleenIsGereedVoorPublicatie);
        Assert.NotNull(result.Autorisaties);
        Assert.Empty(result.Autorisaties);
    }

    /// <summary>
    /// The write direction: the map every POST/PUT/PATCH on APPLICATIE goes through.
    /// <see cref="AcMapsterCompileTests"/> proves it can be built; this proves the values arrive.
    /// </summary>
    [Fact]
    public void Request_dto_maps_to_domain_through_the_real_seam()
    {
        var mapper = MapperThroughTheRealSeam();

        var request = new ApplicatieRequestDto
        {
            Label = "test",
            HeeftAlleAutorisaties = false,
            ClientIds = ["client-a", "client-b"],
            Autorisaties =
            [
                new AutorisatieRequestDto
                {
                    Component = Component.zrc.ToString(),
                    Scopes = ["zaken.lezen"],
                    ZaakType = "https://example.test/zaaktypen/1",
                    MaxVertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.geheim.ToString(),
                },
            ],
        };

        var result = mapper.Map<Applicatie>(request);

        Assert.Equal(["client-a", "client-b"], result.ClientIds.Select(c => c.ClientId));
        Assert.Single(result.Autorisaties);
        Assert.Equal(Component.zrc, result.Autorisaties[0].Component);
        Assert.Equal(VertrouwelijkheidAanduiding.geheim, result.Autorisaties[0].MaxVertrouwelijkheidaanduiding);
        // Ignored members stay default — the handlers own identity, ownership and audit fields.
        Assert.Equal(Guid.Empty, result.Id);
        Assert.Null(result.Owner);
        Assert.Null(result.Autorisaties[0].Owner);
    }

    /// <summary>
    /// ClientIds is assigned in AfterMapping, which the seam.s empty-collection transform does not reach,
    /// so the register handles the null case by hand — asserted here rather than assumed.
    /// </summary>
    [Fact]
    public void Request_dto_without_client_ids_maps_to_an_empty_collection_not_null()
    {
        var mapper = MapperThroughTheRealSeam();

        var result = mapper.Map<Applicatie>(new ApplicatieRequestDto { Label = "test", ClientIds = null });

        Assert.NotNull(result.ClientIds);
        Assert.Empty(result.ClientIds);
    }

    /// <summary>
    /// v1.1 declares its own APPLICATIE request DTO but reuses v1.s AUTORISATIE one.
    /// </summary>
    [Fact]
    public void Request_dto_v1_1_maps_to_domain_through_the_real_seam()
    {
        var mapper = MapperThroughTheRealSeam();

        var request = new Contracts.v1._1.Requests.ApplicatieRequestDto
        {
            Label = "test",
            AlleenIsGereedVoorPublicatie = true,
            ClientIds = ["client-a"],
            Autorisaties = [new AutorisatieRequestDto { Component = Component.ac.ToString(), Scopes = ["autorisaties.lezen"] }],
        };

        var result = mapper.Map<Applicatie>(request);

        Assert.Equal(["client-a"], result.ClientIds.Select(c => c.ClientId));
        Assert.True(result.AlleenIsGereedVoorPublicatie);
        Assert.Single(result.Autorisaties);
        Assert.Equal(Component.ac, result.Autorisaties[0].Component);
        // Not supplied by the request, so the global nullable-enum rule must leave it null rather than
        // substituting the enum's zero value.
        Assert.Null(result.Autorisaties[0].MaxVertrouwelijkheidaanduiding);
    }

    /// <summary>
    /// Builds the mapper as Startup does, so it carries the seam.s global settings rather than a
    /// hand-built subset.
    /// </summary>
    private static IMapper MapperThroughTheRealSeam()
    {
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns("https://example.test/resolved-via-di");

        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);
        services.AddZgwMapster(typeof(DomainToResponseRegister).Assembly, enable: true);

        // Not disposed: the URL resolver pulls IEntityUriService from MapContext lazily at Map()-call time,
        // after this method has returned.
        return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IMapper>();
    }

    /// <summary>
    /// The read-side counterpart to the write-side guard <c>RequestToDomainRegister</c> already carries:
    /// Mapster does not null-guard a member used inside a method call in a <c>.Map(...)</c> lambda, so
    /// <c>src.ClientIds.Select(...)</c> throws <c>ArgumentNullException</c> on a null navigation where
    /// AutoMapper produced an empty collection.
    /// </summary>
    /// <remarks>
    /// Latent rather than reachable today — every handler that maps these DTOs does
    /// <c>.Include(z =&gt; z.ClientIds)</c>, and the request-side <c>.AfterMapping</c> always assigns a
    /// list — but AutoMapper turned a forgotten Include into an empty array and Mapster turns it into a
    /// 500, so the branch needs pinning rather than a comment. Empty, not null, is the AutoMapper
    /// baseline (measured against both mappers). Mutation check: drop a fold and this throws.
    /// </remarks>
    [Fact]
    public void Applicatie_with_an_unloaded_ClientIds_maps_to_an_empty_collection_rather_than_throwing()
    {
        var mapper = MapperThroughTheRealSeam();
        var source = new Applicatie { Id = Guid.NewGuid(), ClientIds = null };

        Assert.Empty(mapper.Map<ApplicatieResponseDto>(source).ClientIds);
        Assert.Empty(mapper.Map<ApplicatieRequestDto>(source).ClientIds);
        Assert.Empty(mapper.Map<OneGround.ZGW.Autorisaties.Contracts.v1._1.Responses.ApplicatieResponseDto>(source).ClientIds);
        Assert.Empty(mapper.Map<OneGround.ZGW.Autorisaties.Contracts.v1._1.Requests.ApplicatieRequestDto>(source).ClientIds);
    }
}
