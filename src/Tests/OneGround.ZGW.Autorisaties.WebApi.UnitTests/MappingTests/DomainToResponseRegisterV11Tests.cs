using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Autorisaties.Contracts.v1._1.Requests;
using OneGround.ZGW.Autorisaties.Contracts.v1._1.Responses;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1._1;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;
using DomainToResponseRegisterV1 = OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1.DomainToResponseRegister;

namespace OneGround.ZGW.Autorisaties.WebApi.UnitTests.MappingTests;

/// <summary>
/// The v1.1 counterpart of <see cref="DomainToResponseProfileTests"/>. v1.1 adds
/// <c>AlleenIsGereedVoorPublicatie</c> to the APPLICATIE contract and reuses v1's AUTORISATIE DTOs, so
/// both registers go into one config here — exactly like the real seam, where <c>config.Scan</c> collects
/// every register in the Web assembly into a single <see cref="TypeAdapterConfig"/>.
/// </summary>
public class DomainToResponseRegisterV11Tests : IDisposable
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly Mock<IEntityUriService> _mockedUriService = new Mock<IEntityUriService>();
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IMapper _mapper;

    public DomainToResponseRegisterV11Tests()
    {
        _mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns("https://example.test/resolved-via-di");

        var config = new TypeAdapterConfig();
        new DomainToResponseRegisterV1().Register(config);
        new DomainToResponseRegister().Register(config);
        config.Compile();

        // MapsterUrlResolver reads IEntityUriService from MapContext, which only ServiceMapper populates
        // from the DI container. The provider/scope must outlive the constructor: the resolver resolves
        // lazily at Map()-call time inside each [Fact].
        var services = new ServiceCollection();
        services.AddSingleton(_mockedUriService.Object);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _mapper = _scope.ServiceProvider.GetRequiredService<IMapper>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Fact]
    public void Applicatie_Maps_To_ApplicatieResponseDto()
    {
        var value = _fixture.Create<Applicatie>();

        var result = _mapper.Map<ApplicatieResponseDto>(value);

        Assert.True(value.ClientIds.All(c => result.ClientIds.Contains(c.ClientId)));
        Assert.Equal(value.HeeftAlleAutorisaties, result.HeeftAlleAutorisaties);
        Assert.Equal(value.AlleenIsGereedVoorPublicatie, result.AlleenIsGereedVoorPublicatie);
        Assert.Equal(value.Label, result.Label);
        // The mock returns a literal unrelated to Applicatie.Url's own computed value, so this only
        // passes if MapsterUrlResolver actually reached IEntityUriService through DI — a same-name
        // convention copy would have yielded the entity's own $"/applicaties/{Id}".
        Assert.Equal("https://example.test/resolved-via-di", result.Url);
    }

    [Fact]
    public void Applicatie_Maps_To_ApplicatieRequestDto()
    {
        // The PATCH path: the existing entity is mapped back onto the request contract before the JSON
        // patch is merged onto it, so every field a client may PATCH must survive this map.
        var value = _fixture.Create<Applicatie>();

        var result = _mapper.Map<ApplicatieRequestDto>(value);

        Assert.True(value.ClientIds.All(c => result.ClientIds.Contains(c.ClientId)));
        Assert.Equal(value.HeeftAlleAutorisaties, result.HeeftAlleAutorisaties);
        Assert.Equal(value.AlleenIsGereedVoorPublicatie, result.AlleenIsGereedVoorPublicatie);
        Assert.Equal(value.Label, result.Label);
    }

    [Fact]
    public void Applicatie_With_Autorisaties_Maps_Nested_Autorisaties_Including_ComponentWeergave()
    {
        var value = new Applicatie
        {
            Id = Guid.NewGuid(),
            Label = "test",
            AlleenIsGereedVoorPublicatie = true,
            ClientIds = new List<ApplicatieClient>(),
            Autorisaties = new List<Autorisatie>
            {
                new Autorisatie { Component = Component.zrc, Scopes = new[] { "zaken.lezen" } },
            },
        };

        var result = _mapper.Map<ApplicatieResponseDto>(value);

        Assert.NotNull(result.Autorisaties);
        Assert.Single(result.Autorisaties);
        Assert.Equal(Component.zrc.ToString(), result.Autorisaties[0].Component);
        // v1.1 declares no AUTORISATIE map of its own — it reuses v1's DTO. ComponentWeergave is only
        // populated if convention-based nested mapping found v1's Autorisatie rule in the shared config.
        Assert.Equal("Zaakregistratiecomponent", result.Autorisaties[0].ComponentWeergave);
    }
}
