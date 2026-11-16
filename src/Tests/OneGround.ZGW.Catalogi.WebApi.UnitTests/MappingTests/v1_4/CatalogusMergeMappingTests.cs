using System;
using MapsterMapper;
using OneGround.ZGW.Catalogi.DataModel;
using Xunit;
using V13Requests = OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using V14 = OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests.v1_4;

/// <summary>
/// The PATCH of a catalogus merges the partial body into the request DTO of the existing catalogus, and the POST/PUT/PATCH of v1.4 answer with the
/// v1.4 response.
/// </summary>
public class CatalogusMergeMappingTests : IDisposable
{
    private readonly ZtcMapperTestHost _host = new();
    private IMapper Mapper => _host.Mapper;

    public void Dispose() => _host.Dispose();

    [Fact]
    public void Catalogus_maps_to_the_request_dto_to_merge_a_partial_update_into()
    {
        var source = new Catalogus
        {
            Id = Guid.NewGuid(),
            Domein = "TST",
            Rsin = "111111111",
            ContactpersoonBeheerNaam = "Naam",
            ContactpersoonBeheerTelefoonnummer = "0612345678",
            ContactpersoonBeheerEmailadres = "a@example.com",
            Naam = "Catalogus",
            Versie = "1.0",
            BegindatumVersie = new DateOnly(2020, 1, 2),
        };

        var result = Mapper.Map<V13Requests.CatalogusRequestDto>(source);

        Assert.Equal("TST", result.Domein);
        Assert.Equal("111111111", result.Rsin);
        Assert.Equal("Naam", result.ContactpersoonBeheerNaam);
        Assert.Equal("0612345678", result.ContactpersoonBeheerTelefoonnummer);
        Assert.Equal("a@example.com", result.ContactpersoonBeheerEmailadres);
        Assert.Equal("Catalogus", result.Naam);
        Assert.Equal("1.0", result.Versie);
        Assert.Equal("2020-01-02", result.BegindatumVersie);
    }

    [Fact]
    public void Catalogus_without_begindatum_versie_maps_to_a_request_dto_without_it()
    {
        var result = Mapper.Map<V13Requests.CatalogusRequestDto>(
            new Catalogus
            {
                Id = Guid.NewGuid(),
                Domein = "TST",
                Rsin = "111111111",
                ContactpersoonBeheerNaam = "Naam",
            }
        );

        Assert.Null(result.BegindatumVersie);
    }

    [Fact]
    public void Created_catalogus_without_types_maps_to_the_v1_4_response_with_empty_lists()
    {
        // As after the POST: the new catalogus has none of its navigations loaded
        var result = Mapper.Map<V14.CatalogusResponseDto>(new Catalogus { Id = Guid.NewGuid(), Domein = "TST" });

        Assert.Empty(result.ZaakTypen);
        Assert.Empty(result.BesluitTypeOmschrijving);
        Assert.Empty(result.InformatieObjectTypeOmschrijving);
    }
}
