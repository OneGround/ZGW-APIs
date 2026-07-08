using System;
using System.Linq;
using AutoFixture;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Autorisaties.Contracts.v1.Requests;
using OneGround.ZGW.Autorisaties.Contracts.v1.Responses;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Autorisaties.WebApi.UnitTests.MappingTests;

public class DomainToResponseProfileTests : IDisposable
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly Mock<IEntityUriService> _mockedUriService = new Mock<IEntityUriService>();
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        _mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        var config = new TypeAdapterConfig();
        new DomainToResponseRegister().Register(config);
        config.Compile();

        // MapsterUrlResolver reads IEntityUriService from MapContext, which ServiceMapper populates
        // from the DI container — so the mapper must be a ServiceMapper with the mock registered.
        // The provider/scope must stay alive for the lifetime of this test class: MapsterUrlResolver
        // resolves IEntityUriService lazily via MapContext at Map()-call time (inside each [Fact]),
        // not eagerly here in the constructor — disposing them here throws ObjectDisposedException
        // once a [Fact] runs.
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
        Assert.Equal(value.Label, result.Label);
        Assert.Equal(value.Url, result.Url);
    }

    [Fact]
    public void Autorisatie_Maps_to_AutorisatieDto()
    {
        var value = _fixture.Create<Autorisatie>();
        var result = _mapper.Map<AutorisatieResponseDto>(value);

        Assert.Equal(value.BesluitType, result.BesluitType);
        Assert.Equal(value.ZaakType, result.ZaakType);
        Assert.Equal(value.InformatieObjectType, result.InformatieObjectType);
        Assert.Equal(value.Component.ToString(), result.Component);
        Assert.Equal(value.MaxVertrouwelijkheidaanduiding.ToString(), result.MaxVertrouwelijkheidaanduiding);
    }

    [Fact]
    public void Applicatie_Maps_to_ApplicatieRequestDto()
    {
        var value = _fixture.Create<Applicatie>();
        var result = _mapper.Map<ApplicatieRequestDto>(value);

        Assert.True(value.ClientIds.All(c => result.ClientIds.Contains(c.ClientId)));
        Assert.Equal(value.HeeftAlleAutorisaties, result.HeeftAlleAutorisaties);
        Assert.Equal(value.Label, result.Label);
    }

    [Fact]
    public void Autorisatie_Maps_to_AutorisatieRequestDto()
    {
        var value = _fixture.Create<Autorisatie>();
        var result = _mapper.Map<AutorisatieRequestDto>(value);

        Assert.Equal(value.BesluitType, result.BesluitType);
        Assert.Equal(value.ZaakType, result.ZaakType);
        Assert.Equal(value.InformatieObjectType, result.InformatieObjectType);
        Assert.Equal(value.Component.ToString(), result.Component);
        Assert.Equal(value.MaxVertrouwelijkheidaanduiding.ToString(), result.MaxVertrouwelijkheidaanduiding);
    }

    [Fact]
    public void Applicatie_With_Autorisaties_Maps_Nested_Autorisaties_Including_ComponentWeergave()
    {
        var value = new Applicatie
        {
            Id = Guid.NewGuid(),
            Label = "test",
            ClientIds = new System.Collections.Generic.List<ApplicatieClient>(),
            Autorisaties = new System.Collections.Generic.List<Autorisatie>
            {
                new Autorisatie { Component = Component.zrc, Scopes = new[] { "zaken.lezen" } },
            },
        };

        var result = _mapper.Map<ApplicatieResponseDto>(value);

        Assert.NotNull(result.Autorisaties);
        Assert.Single(result.Autorisaties);
        Assert.Equal(Component.zrc.ToString(), result.Autorisaties[0].Component);
        // ComponentWeergave is a computed field from the nested Autorisatie->AutorisatieResponseDto rule;
        // it is only populated if convention-based nested mapping used the local config (not GlobalSettings).
        Assert.Equal("Zaakregistratiecomponent", result.Autorisaties[0].ComponentWeergave);
    }
}
