using System;
using MapsterMapper;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Documenten.Web.Models.v1;
using Xunit;
using QueryV1 = OneGround.ZGW.Documenten.Contracts.v1.Queries.GetEnkelvoudigInformatieObjectQueryParameters;
using QueryV15 = OneGround.ZGW.Documenten.Contracts.v1._5.Queries.GetEnkelvoudigInformatieObjectQueryParameters;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests;

public class RequestToPaginationRegisterTests : IDisposable
{
    private readonly DrcMapperTestHost _host = new DrcMapperTestHost();
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

    [Fact]
    public void V1_GetEnkelvoudigInformatieObjectQueryParameters_Maps_RegistratieOp_via_DateTimeFromString()
    {
        var query = new QueryV1 { RegistratieOp = "2024-03-15T10:30:00Z" };

        var result = _mapper.Map<GetEnkelvoudigInformatieObjectFilter>(query);

        Assert.NotNull(result.RegistratieOp);
        Assert.Equal(new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc), result.RegistratieOp.Value.ToUniversalTime());
    }

    [Fact]
    public void V1_GetEnkelvoudigInformatieObjectQueryParameters_with_null_RegistratieOp_maps_to_null()
    {
        var query = new QueryV1 { RegistratieOp = null };

        var result = _mapper.Map<GetEnkelvoudigInformatieObjectFilter>(query);

        Assert.Null(result.RegistratieOp);
    }

    [Fact]
    public void V15_GetEnkelvoudigInformatieObjectQueryParameters_Maps_RegistratieOp_via_DateTimeFromString()
    {
        var query = new QueryV15 { RegistratieOp = "2024-03-15T10:30:00Z" };

        var result = _mapper.Map<GetEnkelvoudigInformatieObjectFilter>(query);

        Assert.NotNull(result.RegistratieOp);
        Assert.Equal(new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc), result.RegistratieOp.Value.ToUniversalTime());
    }
}
