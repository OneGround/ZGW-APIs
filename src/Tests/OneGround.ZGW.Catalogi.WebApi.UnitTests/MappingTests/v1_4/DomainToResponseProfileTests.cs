using System;
using MapsterMapper;
using Newtonsoft.Json;
using OneGround.ZGW.Catalogi.DataModel;
using Xunit;
using V13 = OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using V14 = OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests.v1_4;

/// <summary>
/// The v1.4 responses are the v1.3 responses plus <c>_expand</c>, mapped by inheriting the v1.3 mapping. These tests pin that: the v1.4 mapping has
/// to give exactly the v1.3 response (serialized, so the property values, the order and the omitted <c>_expand</c> are all compared).
/// </summary>
public class DomainToResponseProfileTests : IDisposable
{
    private readonly ZtcMapperTestHost _host = new();
    private IMapper Mapper => _host.Mapper;

    public void Dispose() => _host.Dispose();

    private static string Json(object value) => JsonConvert.SerializeObject(value);

    [Fact]
    public void Catalogus_maps_like_v1_3_without_expand()
    {
        var zaakType = new ZaakType { Id = Guid.NewGuid() };
        var source = new Catalogus
        {
            Id = Guid.NewGuid(),
            Domein = "ABC",
            Rsin = "123456789",
            Naam = "Catalogus",
            ZaakTypes = [zaakType],
            BesluitTypes = [],
            InformatieObjectTypes = [],
        };

        var result = Mapper.Map<V14.CatalogusResponseDto>(source);

        Assert.Null(result.Expand);
        Assert.Equal([ZtcMapperTestHost.Resolved(zaakType)], result.ZaakTypen);
        Assert.Equal(Json(Mapper.Map<V13.CatalogusResponseDto>(source)), Json(result));
    }

    [Fact]
    public void ZaakType_maps_like_v1_3_without_expand()
    {
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
            BeginGeldigheid = new DateOnly(2020, 1, 2),
            VersieDatum = new DateOnly(2020, 1, 6),
            StatusTypen = [],
            RolTypen = [],
            ResultaatTypen = [],
            Eigenschappen = [],
            ZaakObjectTypen = [],
            ZaakTypeGerelateerdeZaakTypen = [],
        };

        var result = Mapper.Map<V14.ZaakTypeResponseDto>(source);

        Assert.Null(result.Expand);
        Assert.Equal("2020-01-02", result.BeginGeldigheid);
        Assert.Equal(Json(Mapper.Map<V13.ZaakTypeResponseDto>(source)), Json(result));
    }

    [Fact]
    public void BesluitType_maps_like_v1_3_without_expand()
    {
        var source = new BesluitType
        {
            Id = Guid.NewGuid(),
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
            BeginGeldigheid = new DateOnly(2020, 1, 2),
            BesluitTypeZaakTypen = [],
            BesluitTypeInformatieObjectTypen = [],
        };

        var result = Mapper.Map<V14.BesluitTypeResponseDto>(source);

        Assert.Null(result.Expand);
        Assert.Equal("2020-01-02", result.BeginGeldigheid);
        Assert.Equal(Json(Mapper.Map<V13.BesluitTypeResponseDto>(source)), Json(result));
    }

    [Fact]
    public void InformatieObjectType_maps_like_v1_3_without_expand()
    {
        var source = new InformatieObjectType
        {
            Id = Guid.NewGuid(),
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
            BeginGeldigheid = new DateOnly(2020, 1, 2),
            InformatieObjectTypeZaakTypen = [],
            InformatieObjectTypeBesluitTypen = [],
        };

        var result = Mapper.Map<V14.InformatieObjectTypeResponseDto>(source);

        Assert.Null(result.Expand);
        Assert.Equal("2020-01-02", result.BeginGeldigheid);
        Assert.Equal(Json(Mapper.Map<V13.InformatieObjectTypeResponseDto>(source)), Json(result));
    }

    [Fact]
    public void Eigenschap_maps_like_v1_3_without_expand()
    {
        var zaakType = new ZaakType
        {
            Id = Guid.NewGuid(),
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
        };
        var source = new Eigenschap
        {
            Id = Guid.NewGuid(),
            Naam = "Eigenschap",
            ZaakType = zaakType,
            BeginGeldigheid = new DateOnly(2020, 1, 2),
        };

        var result = Mapper.Map<V14.EigenschapResponseDto>(source);

        Assert.Null(result.Expand);
        Assert.Equal("2020-01-02", result.BeginGeldigheid);
        Assert.Equal(ZtcMapperTestHost.Resolved(zaakType), result.ZaakType);
        Assert.Equal(Json(Mapper.Map<V13.EigenschapResponseDto>(source)), Json(result));
    }

    [Fact]
    public void ResultaatType_maps_like_v1_3_without_expand()
    {
        var zaakType = new ZaakType
        {
            Id = Guid.NewGuid(),
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
        };
        var besluitType = new BesluitType { Id = Guid.NewGuid() };
        var source = new ResultaatType
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            BeginGeldigheid = new DateOnly(2020, 1, 2),
            ResultaatTypeBesluitTypen = [new ResultaatTypeBesluitType { BesluitType = besluitType }],
        };

        var result = Mapper.Map<V14.ResultaatTypeResponseDto>(source);

        Assert.Null(result.Expand);
        Assert.Equal("2020-01-02", result.BeginGeldigheid);
        Assert.Equal(ZtcMapperTestHost.Resolved(zaakType), result.ZaakType);
        Assert.Equal([ZtcMapperTestHost.Resolved(besluitType)], result.BesluitTypen);
        Assert.Equal(Json(Mapper.Map<V13.ResultaatTypeResponseDto>(source)), Json(result));
    }

    [Fact]
    public void RolType_maps_like_v1_3_without_expand()
    {
        var zaakType = new ZaakType
        {
            Id = Guid.NewGuid(),
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
        };
        var source = new RolType
        {
            Id = Guid.NewGuid(),
            Omschrijving = "Initiator",
            ZaakType = zaakType,
            BeginGeldigheid = new DateOnly(2020, 1, 2),
        };

        var result = Mapper.Map<V14.RolTypeResponseDto>(source);

        Assert.Null(result.Expand);
        Assert.Equal("2020-01-02", result.BeginGeldigheid);
        Assert.Equal(ZtcMapperTestHost.Resolved(zaakType), result.ZaakType);
        Assert.Equal(Json(Mapper.Map<V13.RolTypeResponseDto>(source)), Json(result));
    }

    [Fact]
    public void StatusType_maps_like_v1_3_without_expand()
    {
        var zaakType = new ZaakType
        {
            Id = Guid.NewGuid(),
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
        };
        var eigenschap = new Eigenschap { Id = Guid.NewGuid() };
        var source = new StatusType
        {
            Id = Guid.NewGuid(),
            Omschrijving = "Ontvangen",
            ZaakType = zaakType,
            BeginGeldigheid = new DateOnly(2020, 1, 2),
            StatusTypeVerplichteEigenschappen = [new StatusTypeVerplichteEigenschap { Eigenschap = eigenschap }],
        };

        var result = Mapper.Map<V14.StatusTypeResponseDto>(source);

        Assert.Null(result.Expand);
        Assert.Equal("2020-01-02", result.BeginGeldigheid);
        Assert.Equal(ZtcMapperTestHost.Resolved(zaakType), result.ZaakType);
        Assert.Equal([ZtcMapperTestHost.Resolved(eigenschap)], result.Eigenschappen);
        Assert.Equal(Json(Mapper.Map<V13.StatusTypeResponseDto>(source)), Json(result));
    }

    [Fact]
    public void StatusType_without_loaded_eigenschappen_keeps_them_null_like_v1_3()
    {
        // Note: the v1.3 mapping fills the eigenschappen in an AfterMapping so that they stay null when they are not loaded; the v1.4 mapping inherits that
        var source = new StatusType
        {
            Id = Guid.NewGuid(),
            ZaakType = new ZaakType
            {
                Id = Guid.NewGuid(),
                Catalogus = new Catalogus { Id = Guid.NewGuid() },
            },
            StatusTypeVerplichteEigenschappen = null,
        };

        var result = Mapper.Map<V14.StatusTypeResponseDto>(source);

        Assert.Null(result.Eigenschappen);
        Assert.Equal(Json(Mapper.Map<V13.StatusTypeResponseDto>(source)), Json(result));
    }

    [Fact]
    public void ResultaatType_without_loaded_besluittypen_keeps_them_null_like_v1_3()
    {
        // Note: the v1.3 mapping fills the besluittypen in an AfterMapping so that they stay null when they are not loaded; the v1.4 mapping inherits that
        var source = new ResultaatType
        {
            Id = Guid.NewGuid(),
            ZaakType = new ZaakType
            {
                Id = Guid.NewGuid(),
                Catalogus = new Catalogus { Id = Guid.NewGuid() },
            },
            ResultaatTypeBesluitTypen = null,
        };

        var result = Mapper.Map<V14.ResultaatTypeResponseDto>(source);

        Assert.Null(result.BesluitTypen);
        Assert.Equal(Json(Mapper.Map<V13.ResultaatTypeResponseDto>(source)), Json(result));
    }

    [Fact]
    public void ZaakObjectType_maps_like_v1_3_without_expand()
    {
        var zaakType = new ZaakType
        {
            Id = Guid.NewGuid(),
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
        };
        var source = new ZaakObjectType
        {
            Id = Guid.NewGuid(),
            AnderObjectType = true,
            ObjectType = "https://objecttypes.test/objecttypes/1",
            ZaakType = zaakType,
            BeginGeldigheid = new DateOnly(2020, 1, 2),
        };

        var result = Mapper.Map<V14.ZaakObjectTypeResponseDto>(source);

        Assert.Null(result.Expand);
        Assert.Equal("2020-01-02", result.BeginGeldigheid);
        Assert.Equal(ZtcMapperTestHost.Resolved(zaakType), result.ZaakType);
        Assert.Equal(Json(Mapper.Map<V13.ZaakObjectTypeResponseDto>(source)), Json(result));
    }
}
