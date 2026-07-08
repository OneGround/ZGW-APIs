using Mapster;
using MapsterMapper;
using OneGround.ZGW.Autorisaties.Web.MappingProfiles;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Models;
using Xunit;

namespace OneGround.ZGW.Autorisaties.WebApi.UnitTests.MappingTests;

public class RequestToPaginationRegisterTests
{
    private readonly IMapper _mapper;

    public RequestToPaginationRegisterTests()
    {
        var config = new TypeAdapterConfig();
        new RequestToPaginationRegister().Register(config);
        config.Compile();
        _mapper = new Mapper(config);
    }

    [Fact]
    public void PaginationQuery_Maps_To_PaginationFilter()
    {
        var query = new PaginationQuery(page: 3, pageSize: 25);

        var result = _mapper.Map<PaginationFilter>(query);

        Assert.Equal(query.Page, result.Page);
        Assert.Equal(query.Size, result.Size);
    }
}
