using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Expands;

public class ExpandQueryStatusTests
{
    [Fact]
    public void IsAvailable_Ok_IsTrue()
    {
        Assert.True(ExpandQueryStatus.IsAvailable(QueryStatus.OK, "zaak"));
    }

    [Theory]
    [InlineData(QueryStatus.NotFound)]
    [InlineData(QueryStatus.Forbidden)]
    public void IsAvailable_NotAvailableToTheCaller_IsFalse(QueryStatus status)
    {
        Assert.False(ExpandQueryStatus.IsAvailable(status, "zaak"));
    }

    [Theory]
    [InlineData(QueryStatus.Failed)]
    [InlineData(QueryStatus.ValidationError)]
    [InlineData(QueryStatus.ExternalServiceFailure)]
    [InlineData(QueryStatus.Unspecified)]
    public void IsAvailable_RealFailure_Throws(QueryStatus status)
    {
        var exception = Assert.Throws<ExpandInternalQueryHandlerException>(() => ExpandQueryStatus.IsAvailable(status, "zaak"));

        Assert.Equal("zaak", exception.Resource);
        Assert.Equal(status, exception.StatusCode);
    }
}
