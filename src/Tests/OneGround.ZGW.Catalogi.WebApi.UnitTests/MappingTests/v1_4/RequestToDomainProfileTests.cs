using System;
using MapsterMapper;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Models.v1._3;
using Xunit;
using BesluitTypenQueryParameters = OneGround.ZGW.Catalogi.Contracts.v1._4.Queries.GetAllBesluitTypenQueryParameters;
using CatalogussenFilter = OneGround.ZGW.Catalogi.Web.Models.v1.GetAllCatalogussenFilter;
using CatalogussenQueryParameters = OneGround.ZGW.Catalogi.Contracts.v1._4.Queries.GetAllCatalogussenQueryParameters;
using EigenschappenQueryParameters = OneGround.ZGW.Catalogi.Contracts.v1._4.Queries.GetAllEigenschappenQueryParameters;
using InformatieObjectTypenFilter = OneGround.ZGW.Catalogi.Web.Models.v1.GetAllInformatieObjectTypenFilter;
using InformatieObjectTypenQueryParameters = OneGround.ZGW.Catalogi.Contracts.v1._4.Queries.GetAllInformatieObjectTypenQueryParameters;
using ResultaatTypenQueryParameters = OneGround.ZGW.Catalogi.Contracts.v1._4.Queries.GetAllResultaatTypenQueryParameters;
using RolTypenQueryParameters = OneGround.ZGW.Catalogi.Contracts.v1._4.Queries.GetAllRolTypenQueryParameters;
using StatusTypenQueryParameters = OneGround.ZGW.Catalogi.Contracts.v1._4.Queries.GetAllStatusTypenQueryParameters;
using ZaakObjectTypenQueryParameters = OneGround.ZGW.Catalogi.Contracts.v1._4.Queries.GetAllZaakObjectTypenQueryParameters;
using ZaakTypenFilter = OneGround.ZGW.Catalogi.Web.Models.v1.GetAllZaakTypenFilter;
using ZaakTypenQueryParameters = OneGround.ZGW.Catalogi.Contracts.v1._4.Queries.GetAllZaakTypenQueryParameters;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests.v1_4;

/// <summary>
/// The v1.4 query parameters of the lists (v1.3 plus <c>expand</c>) are mapped to the filters that the v1.3 handlers use. A registration that falls
/// back to Mapster's default mapping would silently ignore the filter, so each filter that needs a conversion is pinned here.
/// </summary>
public class RequestToDomainProfileTests : IDisposable
{
    private readonly ZtcMapperTestHost _host = new();
    private IMapper Mapper => _host.Mapper;

    public void Dispose() => _host.Dispose();

    [Fact]
    public void Catalogussen_maps_the_lists_of_domeinen_and_rsins()
    {
        var source = new CatalogussenQueryParameters { Domein__in = "ABC,DEF", Rsin__in = "123456782,111222333" };

        var result = Mapper.Map<CatalogussenFilter>(source);

        Assert.Equal(["ABC", "DEF"], result.Domein__in);
        Assert.Equal(["123456782", "111222333"], result.Rsin__in);
    }

    [Fact]
    public void ZaakTypen_maps_the_trefwoorden_and_the_date()
    {
        var source = new ZaakTypenQueryParameters { Trefwoorden = "a,b", DatumGeldigheid = "2024-03-15" };

        var result = Mapper.Map<ZaakTypenFilter>(source);

        Assert.Equal(["a", "b"], result.Trefwoorden);
        Assert.Equal(new DateOnly(2024, 3, 15), result.DatumGeldigheid);
    }

    [Fact]
    public void ZaakTypen_maps_the_status()
    {
        var result = Mapper.Map<ZaakTypenFilter>(new ZaakTypenQueryParameters { Status = "concept" });

        Assert.Equal(ConceptStatus.concept, result.Status);
    }

    [Fact]
    public void InformatieObjectTypen_maps_the_date()
    {
        var result = Mapper.Map<InformatieObjectTypenFilter>(new InformatieObjectTypenQueryParameters { DatumGeldigheid = "2024-03-15" });

        Assert.Equal(new DateOnly(2024, 3, 15), result.DatumGeldigheid);
    }

    [Fact]
    public void BesluitTypen_maps_the_date()
    {
        var result = Mapper.Map<GetAllBesluitTypenFilter>(new BesluitTypenQueryParameters { DatumGeldigheid = "2024-03-15" });

        Assert.Equal(new DateOnly(2024, 3, 15), result.DatumGeldigheid);
    }

    [Fact]
    public void Eigenschappen_maps_the_date()
    {
        var result = Mapper.Map<GetAllEigenschappenFilter>(new EigenschappenQueryParameters { DatumGeldigheid = "2024-03-15" });

        Assert.Equal(new DateOnly(2024, 3, 15), result.DatumGeldigheid);
    }

    [Fact]
    public void ResultaatTypen_maps_the_date()
    {
        var result = Mapper.Map<GetAllResultaatTypenFilter>(new ResultaatTypenQueryParameters { DatumGeldigheid = "2024-03-15" });

        Assert.Equal(new DateOnly(2024, 3, 15), result.DatumGeldigheid);
    }

    [Fact]
    public void RolTypen_maps_the_date()
    {
        var result = Mapper.Map<GetAllRolTypenFilter>(new RolTypenQueryParameters { DatumGeldigheid = "2024-03-15" });

        Assert.Equal(new DateOnly(2024, 3, 15), result.DatumGeldigheid);
    }

    [Fact]
    public void StatusTypen_maps_the_date()
    {
        var result = Mapper.Map<GetAllStatusTypenFilter>(new StatusTypenQueryParameters { DatumGeldigheid = "2024-03-15" });

        Assert.Equal(new DateOnly(2024, 3, 15), result.DatumGeldigheid);
    }

    [Fact]
    public void ZaakObjectTypen_maps_the_boolean_and_the_dates()
    {
        var source = new ZaakObjectTypenQueryParameters
        {
            AnderObjectType = "true",
            DatumBeginGeldigheid = "2024-01-01",
            DatumEindeGeldigheid = "2024-12-31",
            DatumGeldigheid = "2024-03-15",
        };

        var result = Mapper.Map<GetAllZaakObjectTypenFilter>(source);

        Assert.True(result.AnderObjectType);
        Assert.Equal(new DateOnly(2024, 1, 1), result.DatumBeginGeldigheid);
        Assert.Equal(new DateOnly(2024, 12, 31), result.DatumEindeGeldigheid);
        Assert.Equal(new DateOnly(2024, 3, 15), result.DatumGeldigheid);
    }

    [Fact]
    public void ZaakObjectTypen_without_anderObjecttype_maps_to_null()
    {
        Assert.Null(Mapper.Map<GetAllZaakObjectTypenFilter>(new ZaakObjectTypenQueryParameters { AnderObjectType = null }).AnderObjectType);
    }

    [Fact]
    public void A_date_that_cannot_be_parsed_maps_to_no_date()
    {
        // Note: the validator rejects such a date before the mapping is reached (see the validator tests); the mapping itself leaves it out
        Assert.Null(Mapper.Map<ZaakTypenFilter>(new ZaakTypenQueryParameters { DatumGeldigheid = "not-a-date" }).DatumGeldigheid);
        Assert.Null(
            Mapper.Map<InformatieObjectTypenFilter>(new InformatieObjectTypenQueryParameters { DatumGeldigheid = "not-a-date" }).DatumGeldigheid
        );
        Assert.Null(Mapper.Map<GetAllBesluitTypenFilter>(new BesluitTypenQueryParameters { DatumGeldigheid = "not-a-date" }).DatumGeldigheid);
        Assert.Null(Mapper.Map<GetAllEigenschappenFilter>(new EigenschappenQueryParameters { DatumGeldigheid = "not-a-date" }).DatumGeldigheid);
        Assert.Null(Mapper.Map<GetAllResultaatTypenFilter>(new ResultaatTypenQueryParameters { DatumGeldigheid = "not-a-date" }).DatumGeldigheid);
        Assert.Null(Mapper.Map<GetAllRolTypenFilter>(new RolTypenQueryParameters { DatumGeldigheid = "not-a-date" }).DatumGeldigheid);
        Assert.Null(Mapper.Map<GetAllStatusTypenFilter>(new StatusTypenQueryParameters { DatumGeldigheid = "not-a-date" }).DatumGeldigheid);
    }
}
