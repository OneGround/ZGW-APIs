using System;
using System.Collections.Generic;
using MapsterMapper;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests.v1_7;

// Discriminates that MappingProfiles.v1._7.DomainToResponseRegister actually registers its own
// ZaakStatus->v1._7.StatusResponseDto config for the v1._7 concrete type, rather than silently
// falling through to Mapster's naive convention mapping (which would not resolve Url/Zaak/
// ZaakInformatieObjecten through IEntityUriService the way the custom .Map calls do).
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
    public void ZaakStatus_Maps_To_v1_7_StatusResponseDto()
    {
        var zaak = new Zaak { Id = Guid.NewGuid(), ZaakInformatieObjecten = [new ZaakInformatieObject { Id = Guid.NewGuid() }] };
        var zaakStatus = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ZaakId = zaak.Id,
            StatusType = "https://catalogi.test/statustypen/1",
            DatumStatusGezet = new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc),
            StatusToelichting = "Toelichting",
            IndicatieLaatstGezetteStatus = true,
            GezetDoor = "https://zrc.test/rollen/1",
        };

        var result = _mapper.Map<StatusResponseDto>(zaakStatus);

        Assert.Equal(ZrcMapperTestHost.Resolved(zaakStatus), result.Url);
        Assert.Equal(zaakStatus.Id.ToString(), result.Uuid);
        Assert.Equal(ZrcMapperTestHost.Resolved(zaak), result.Zaak);
        Assert.Equal(zaakStatus.StatusType, result.StatusType);
        Assert.Equal(zaakStatus.StatusToelichting, result.StatusToelichting);
        Assert.Equal(zaakStatus.IndicatieLaatstGezetteStatus, result.IndicatieLaatstGezetteStatus);
        Assert.Equal(zaakStatus.GezetDoor, result.GezetDoor);
        Assert.Equal([ZrcMapperTestHost.Resolved(zaak.ZaakInformatieObjecten[0])], result.ZaakInformatieObjecten);
        Assert.Null(result.Expand);
    }
}
