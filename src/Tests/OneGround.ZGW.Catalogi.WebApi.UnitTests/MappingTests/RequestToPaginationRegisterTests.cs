using System;
using Mapster;
using MapsterMapper;
using OneGround.ZGW.Catalogi.Web.MappingProfiles;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Models;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests;

public class RequestToPaginationRegisterTests : IDisposable
{
    private readonly ZtcMapperTestHost _host = new ZtcMapperTestHost();
    private readonly IMapper _mapper;

    public RequestToPaginationRegisterTests()
    {
        _mapper = _host.Mapper;
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void PaginationQuery_Maps_To_PaginationFilter()
    {
        var query = new PaginationQuery(page: 3, pageSize: 42);

        var result = _mapper.Map<PaginationFilter>(query);

        Assert.Equal(3, result.Page);
        Assert.Equal(42, result.Size);
    }
}
