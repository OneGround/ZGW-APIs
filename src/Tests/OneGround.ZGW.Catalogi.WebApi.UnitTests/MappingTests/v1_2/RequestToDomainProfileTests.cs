using System;
using Mapster;
using MapsterMapper;
using OneGround.ZGW.Catalogi.Contracts.v1._2.Queries;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._2;
using OneGround.ZGW.Catalogi.Web.Models.v1;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests.v1_2;

public class RequestToDomainProfileTests
{
    private readonly IMapper _mapper;

    public RequestToDomainProfileTests()
    {
        var config = new TypeAdapterConfig();
        new RequestToDomainRegister().Register(config);
        config.Compile();
        _mapper = new Mapper(config);
    }

    [Fact]
    public void GetAllInformatieObjectTypenQueryParameters_Maps_To_GetAllInformatieObjectTypenFilter()
    {
        var value = new GetAllInformatieObjectTypenQueryParameters
        {
            Catalogus = "https://example.test/catalogussen/abc",
            Status = "definitief",
            DatumGeldigheid = "2024-03-15",
            Omschrijving = "some description",
        };

        var result = _mapper.Map<GetAllInformatieObjectTypenFilter>(value);

        Assert.Equal(value.Catalogus, result.Catalogus);
        Assert.Equal(value.Omschrijving, result.Omschrijving);
        Assert.Equal(new DateOnly(2024, 3, 15), result.DatumGeldigheid);
        // Status is a non-nullable enum, so this exercises Mapster's own built-in string->enum-name
        // conversion, not the seam's RegisterNullableEnumRule (which only matches Nullable<enum>
        // destinations) or NameMatchingStrategy.IgnoreCase (both members are already same-named,
        // same-case) — both are irrelevant here and neither is active in this bare TypeAdapterConfig.
        Assert.Equal(ConceptStatus.definitief, result.Status);
    }

    [Fact]
    public void GetAllInformatieObjectTypenQueryParameters_with_unparseable_date_maps_DatumGeldigheid_to_null()
    {
        var value = new GetAllInformatieObjectTypenQueryParameters { DatumGeldigheid = "not-a-date" };

        var result = _mapper.Map<GetAllInformatieObjectTypenFilter>(value);

        Assert.Null(result.DatumGeldigheid);
    }
}
