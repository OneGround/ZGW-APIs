using System;
using MapsterMapper;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.Contracts.v1._7.Requests;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Models.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests.v1_7;

public class ZaakNotitieProfileTests : IDisposable
{
    private readonly ZrcMapperTestHost _host = new();
    private readonly IMapper _mapper;

    public ZaakNotitieProfileTests()
    {
        _mapper = _host.Mapper;
    }

    public void Dispose() => _host.Dispose();

    private static ZaakNotitie Notitie(DateTime? modificationTime = null)
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };

        return new ZaakNotitie
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ZaakId = zaak.Id,
            Onderwerp = "Onderwerp",
            Tekst = "Tekst",
            AangemaaktDoor = "Auteur",
            NotitieType = ZaakNotitieType.@extern,
            Status = ZaakNotitieStatus.definitief,
            CreationTime = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc),
            ModificationTime = modificationTime,
        };
    }

    [Fact]
    public void Request_WithoutTypeAndStatus_MapsToAnInternConcept()
    {
        var result = _mapper.Map<ZaakNotitie>(new ZaakNotitieRequestDto { Onderwerp = "O", Tekst = "T" });

        Assert.Equal(ZaakNotitieType.intern, result.NotitieType);
        Assert.Equal(ZaakNotitieStatus.concept, result.Status);
        Assert.Equal(Guid.Empty, result.Id);
    }

    [Fact]
    public void Request_WithTypeAndStatus_MapsTheEnums()
    {
        var result = _mapper.Map<ZaakNotitie>(
            new ZaakNotitieRequestDto
            {
                Onderwerp = "O",
                Tekst = "T",
                AangemaaktDoor = "A",
                NotitieType = "extern",
                Status = "definitief",
            }
        );

        Assert.Equal(ZaakNotitieType.@extern, result.NotitieType);
        Assert.Equal(ZaakNotitieStatus.definitief, result.Status);
        Assert.Equal("A", result.AangemaaktDoor);
    }

    [Fact]
    public void Response_ResolvesTheUrls_AndUsesTheCreationTimeAsLastModifiedWhenNeverModified()
    {
        var notitie = Notitie(modificationTime: null);

        var result = _mapper.Map<ZaakNotitieResponseDto>(notitie);

        Assert.Equal(ZrcMapperTestHost.Resolved(notitie), result.Url);
        Assert.Equal(ZrcMapperTestHost.Resolved(notitie.Zaak), result.GerelateerdAan);
        Assert.Equal("extern", result.NotitieType);
        Assert.Equal("definitief", result.Status);
        Assert.Equal(notitie.CreationTime, result.Aanmaakdatum);
        Assert.Equal(notitie.CreationTime, result.Wijzigingsdatum);
    }

    [Fact]
    public void Response_UsesTheModificationTimeAsLastModified()
    {
        var modified = new DateTime(2026, 10, 2, 9, 30, 0, DateTimeKind.Utc);

        var result = _mapper.Map<ZaakNotitieResponseDto>(Notitie(modified));

        Assert.Equal(modified, result.Wijzigingsdatum);
    }

    [Fact]
    public void ExistingNotitie_MapsToARequest_ForTheMergeOfAPatch()
    {
        var notitie = Notitie();

        var result = _mapper.Map<ZaakNotitieRequestDto>(notitie);

        Assert.Equal(ZrcMapperTestHost.Resolved(notitie.Zaak), result.GerelateerdAan);
        Assert.Equal("Onderwerp", result.Onderwerp);
        Assert.Equal("extern", result.NotitieType);
    }

    [Fact]
    public void QueryParameters_MapToTheFilter()
    {
        var result = _mapper.Map<GetAllZaakNotitiesFilter>(
            new GetAllZaakNotitiesQueryParameters
            {
                Onderwerp__icontains = "abc",
                NotitieType = "intern",
                Status = "concept",
                Aanmaakdatum__gte = "2026-10-01T08:00:00Z",
                Wijzigingsdatum__lt = "2026-10-02T10:00:00+02:00",
            }
        );

        Assert.Equal("abc", result.Onderwerp__icontains);
        Assert.Equal(ZaakNotitieType.intern, result.NotitieType);
        Assert.Equal(ZaakNotitieStatus.concept, result.Status);
        Assert.Equal(new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc), result.Aanmaakdatum__gte);
        Assert.Equal(new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc), result.Wijzigingsdatum__lt);
        Assert.Null(result.Aanmaakdatum);
    }

    [Fact]
    public void QueryParameters_WithoutEnums_MapToNullEnums()
    {
        var result = _mapper.Map<GetAllZaakNotitiesFilter>(new GetAllZaakNotitiesQueryParameters());

        Assert.Null(result.NotitieType);
        Assert.Null(result.Status);
    }
}
