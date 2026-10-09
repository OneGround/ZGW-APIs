using System;
using MapsterMapper;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests.v1_7;

// Discriminates that MappingProfiles.v1._7.DomainToResponseRegister registers its own
// ZaakRol->v1._7.RolResponseDto config (mirroring v1._5's ConstructUsing-based subtype dispatch),
// rather than silently falling through to Mapster's naive convention mapping (which would not
// produce the correct BetrokkeneIdentificatie-carrying subtype instance).
public class RolResponseDtoProfileTests : IDisposable
{
    private readonly ZrcMapperTestHost _host = new();
    private readonly IMapper _mapper;

    public RolResponseDtoProfileTests()
    {
        _mapper = _host.Mapper;
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void ZaakRol_Medewerker_Maps_To_MedewerkerRolResponseDto_WithBetrokkeneIdentificatie()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var zaakRol = new ZaakRol
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ZaakId = zaak.Id,
            BetrokkeneType = BetrokkeneType.medewerker,
            RolType = "https://catalogi.test/roltypen/1",
            Roltoelichting = "Toelichting",
            Omschrijving = "Behandelaar",
            OmschrijvingGeneriek = OmschrijvingGeneriek.behandelaar,
            Medewerker = new MedewerkerZaakRol { Identificatie = "medewerker-123", Achternaam = "Jansen" },
        };

        var result = _mapper.Map<RolResponseDto>(zaakRol);

        var medewerkerResult = Assert.IsType<MedewerkerRolResponseDto>(result);
        Assert.Equal(ZrcMapperTestHost.Resolved(zaakRol), medewerkerResult.Url);
        Assert.Equal(zaakRol.Id.ToString(), medewerkerResult.Uuid);
        Assert.Equal(ZrcMapperTestHost.Resolved(zaak), medewerkerResult.Zaak);
        Assert.Equal(zaakRol.RolType, medewerkerResult.RolType);
        Assert.Equal("medewerker-123", medewerkerResult.BetrokkeneIdentificatie.Identificatie);
        Assert.Equal("Jansen", medewerkerResult.BetrokkeneIdentificatie.Achternaam);
        Assert.Null(medewerkerResult.Expand);
    }

    [Fact]
    public void ZaakRol_NatuurlijkPersoon_WithoutNavigationLoaded_Maps_To_SubtypeWithNullBetrokkeneIdentificatie()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var zaakRol = new ZaakRol
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ZaakId = zaak.Id,
            BetrokkeneType = BetrokkeneType.natuurlijk_persoon,
            RolType = "https://catalogi.test/roltypen/1",
            Roltoelichting = "Toelichting",
            Omschrijving = "Initiator",
            OmschrijvingGeneriek = OmschrijvingGeneriek.initiator,
            NatuurlijkPersoon = null,
        };

        var result = _mapper.Map<RolResponseDto>(zaakRol);

        var natuurlijkPersoonResult = Assert.IsType<NatuurlijkPersoonRolResponseDto>(result);
        Assert.Null(natuurlijkPersoonResult.BetrokkeneIdentificatie);
    }
}
