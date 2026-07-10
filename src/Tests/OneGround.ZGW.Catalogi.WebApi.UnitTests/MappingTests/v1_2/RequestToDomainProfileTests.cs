using Mapster;
using MapsterMapper;
using OneGround.ZGW.Catalogi.Contracts.v1._2.Queries;
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
        Assert.Equal(new System.DateOnly(2024, 3, 15), result.DatumGeldigheid);
        Assert.Equal(OneGround.ZGW.Catalogi.DataModel.ConceptStatus.definitief, result.Status);
    }

    [Fact]
    public void GetAllInformatieObjectTypenQueryParameters_with_unparseable_date_maps_DatumGeldigheid_to_null()
    {
        var value = new GetAllInformatieObjectTypenQueryParameters { DatumGeldigheid = "not-a-date" };

        var result = _mapper.Map<GetAllInformatieObjectTypenFilter>(value);

        Assert.Null(result.DatumGeldigheid);
    }
}
