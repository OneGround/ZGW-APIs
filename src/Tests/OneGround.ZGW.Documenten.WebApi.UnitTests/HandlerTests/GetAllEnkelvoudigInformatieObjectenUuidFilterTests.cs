using System;
using System.Linq;
using OneGround.ZGW.Documenten.DataModel;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.HandlerTests;

/// <summary>
/// The Uuid__in filter of the _zoek query: only the objects whose id is listed are returned.
/// </summary>
public class GetAllEnkelvoudigInformatieObjectenUuidFilterTests
{
    private static readonly EnkelvoudigInformatieObject Matching = new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111") };
    private static readonly EnkelvoudigInformatieObject Other = new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222") };

    [Fact]
    public void V1_5_Uuid__in_returns_only_the_matching_object()
    {
        var filter = new Web.Models.v1._5.GetAllEnkelvoudigInformatieObjectenFilter { Uuid__in = [Matching.Id.ToString()] };

        var predicate = Web.Handlers.v1._5.GetAllEnkelvoudigInformatieObjectenQueryHandler.GetEnkelvoudigInformatieObjectFilterPredicate(filter);

        Assert.Equal([Matching], new[] { Matching, Other }.AsQueryable().Where(predicate).ToList());
    }

    [Fact]
    public void V1_7_Uuid__in_returns_only_the_matching_object()
    {
        var filter = new Web.Models.v1._7.GetAllEnkelvoudigInformatieObjectenFilter { Uuid__in = [Matching.Id.ToString()] };

        var predicate = Web.Handlers.v1._7.GetAllEnkelvoudigInformatieObjectenQueryHandler.GetEnkelvoudigInformatieObjectFilterPredicate(filter);

        Assert.Equal([Matching], new[] { Matching, Other }.AsQueryable().Where(predicate).ToList());
    }
}
