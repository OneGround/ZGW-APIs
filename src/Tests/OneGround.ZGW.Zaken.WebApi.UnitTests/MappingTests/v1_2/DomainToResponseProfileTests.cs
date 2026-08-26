using System;
using MapsterMapper;
using Moq;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Zaken.Contracts.v1.Requests;
using OneGround.ZGW.Zaken.DataModel;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests.v1_2;

public class DomainToResponseProfileTests : IDisposable
{
    private readonly ZrcMapperTestHost _host = new();
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        _mapper = _host.Mapper;
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void ZaakEigenschap_Maps_To_ZaakEigenschapRequestDto()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var source = new ZaakEigenschap
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            Naam = "eigenschap-naam",
            Waarde = "eigenschap-waarde",
        };

        var result = _mapper.Map<ZaakEigenschapRequestDto>(source);

        // The host's mock PREFIXES the entity's relative Url, so the expected value is distinguishable
        // from any same-name convention copy of src.Zaak's own Url - this only passes if MemberUrlResolver
        // was correctly ported to MapsterUrlResolver.ResolveUrl(src.Zaak) resolving IEntityUriService
        // through DI.
        Assert.Equal(ZrcMapperTestHost.Resolved(zaak), result.Zaak);
        _host.UriService.Verify(s => s.GetUri(It.IsAny<IUrlEntity>()), Times.AtLeastOnce());
    }

    [Fact]
    public void ZaakEigenschap_with_null_Zaak_maps_to_null()
    {
        var source = new ZaakEigenschap
        {
            Id = Guid.NewGuid(),
            Zaak = null,
            Naam = "eigenschap-naam",
            Waarde = "eigenschap-waarde",
        };

        var result = _mapper.Map<ZaakEigenschapRequestDto>(source);

        Assert.Null(result.Zaak);
    }
}
