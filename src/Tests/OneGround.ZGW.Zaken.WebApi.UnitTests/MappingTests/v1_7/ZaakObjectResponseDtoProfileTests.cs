using System;
using MapsterMapper;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakObject;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests.v1_7;

// Discriminates that MappingProfiles.v1._7.DomainToResponseRegister registers its own
// ZaakObject->v1._7.ZaakObjectResponseDto config (mirroring v1._5's ConstructUsing-based subtype
// dispatch), rather than silently falling through to Mapster's naive convention mapping (which would
// not produce the correct ObjectIdentificatie-carrying subtype instance).
public class ZaakObjectResponseDtoProfileTests : IDisposable
{
    private readonly ZrcMapperTestHost _host = new();
    private readonly IMapper _mapper;

    public ZaakObjectResponseDtoProfileTests()
    {
        _mapper = _host.Mapper;
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void ZaakObject_Adres_Maps_To_AdresZaakObjectResponseDto_WithObjectIdentificatie()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var zaakObject = new ZaakObject
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ZaakId = zaak.Id,
            ObjectType = ObjectType.adres,
            Adres = new AdresZaakObject
            {
                Identificatie = "adres-123",
                WplWoonplaatsNaam = "Utrecht",
                GorOpenbareRuimteNaam = "Kerkstraat",
                Huisnummer = 1,
            },
        };

        var result = _mapper.Map<ZaakObjectResponseDto>(zaakObject);

        var adresResult = Assert.IsType<AdresZaakObjectResponseDto>(result);
        Assert.Equal(ZrcMapperTestHost.Resolved(zaakObject), adresResult.Url);
        Assert.Equal(zaakObject.Id, adresResult.Uuid);
        Assert.Equal(ZrcMapperTestHost.Resolved(zaak), adresResult.Zaak);
        Assert.Equal("adres-123", adresResult.ObjectIdentificatie.Identificatie);
        Assert.Null(adresResult.Expand);
    }

    [Fact]
    public void ZaakObject_Pand_WithoutNavigationLoaded_Maps_To_SubtypeWithNullObjectIdentificatie()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var zaakObject = new ZaakObject
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ZaakId = zaak.Id,
            ObjectType = ObjectType.pand,
            Pand = null,
        };

        var result = _mapper.Map<ZaakObjectResponseDto>(zaakObject);

        var pandResult = Assert.IsType<PandZaakObjectResponseDto>(result);
        Assert.Null(pandResult.ObjectIdentificatie);
    }

    // Unlike ROL's BetrokkeneType (5 values, all mapped to a concrete subtype), ZAAKOBJECT's ObjectType
    // has ~26 values but only 8 concrete ObjectIdentificatie subtypes -- most values (e.g. "besluit")
    // fall through to the plain base DTO, exactly like v1._5's own CreateZaakObjectResponseDto.
    [Fact]
    public void ZaakObject_UnmappedObjectType_Maps_To_BaseZaakObjectResponseDto()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var zaakObject = new ZaakObject
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ZaakId = zaak.Id,
            ObjectType = ObjectType.besluit,
        };

        var result = _mapper.Map<ZaakObjectResponseDto>(zaakObject);

        Assert.IsType<ZaakObjectResponseDto>(result);
    }
}
