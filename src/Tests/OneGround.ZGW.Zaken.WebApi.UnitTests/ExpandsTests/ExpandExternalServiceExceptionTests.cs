using System;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.ServiceAgent.Expands;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ExpandExternalServiceExceptionTests
{
    private const string Url = "https://drc.test/enkelvoudiginformatieobjecten/11111111-1111-1111-1111-111111111111";

    [Fact]
    public void ForFailedResponse_KeepsStatusReasonAndExceptionOfTheServiceAgent()
    {
        var cause = new TimeoutException("timeout");
        var response = new ServiceAgentResponse(new ErrorResponse { Status = 500, Title = "De URL x gaf als antwoord HTTP 500." }, cause);

        var exception = ExpandExternalServiceException.ForFailedResponse("DRC", Url, response);

        Assert.Equal("DRC", exception.ServiceName);
        Assert.Equal(Url, exception.ServiceUrl);
        Assert.Equal(500, exception.StatusCode);
        Assert.Same(cause, exception.InnerException);
        Assert.Contains("(HTTP 500)", exception.Message);
        Assert.Contains("De URL x gaf als antwoord HTTP 500.", exception.Message);
        Assert.Contains(Url, exception.Message);
    }

    [Fact]
    public void ForFailedResponse_ServiceDidNotAnswer_HasNoStatus()
    {
        var response = new ServiceAgentResponse(null, new TimeoutException("timeout"));

        var exception = ExpandExternalServiceException.ForFailedResponse("DRC", Url, response);

        Assert.Null(exception.StatusCode);
        Assert.IsType<TimeoutException>(exception.InnerException);
        Assert.DoesNotContain("HTTP", exception.Message);
    }

    [Fact]
    public void ForFailedResponse_ErrorWithoutAStatus_HasNoStatus()
    {
        var exception = ExpandExternalServiceException.ForFailedResponse("DRC", Url, new ServiceAgentResponse(new ErrorResponse(), null));

        Assert.Null(exception.StatusCode);
    }

    [Fact]
    public void Constructor_WithInnerException_KeepsItAndHasNoStatus()
    {
        var cause = new InvalidOperationException("boom");

        var exception = new ExpandExternalServiceException("ZTC", Url, cause);

        Assert.Same(cause, exception.InnerException);
        Assert.Null(exception.StatusCode);
    }

    [Theory]
    [InlineData(403, true)]
    [InlineData(404, true)]
    [InlineData(400, false)]
    [InlineData(500, false)]
    [InlineData(502, false)]
    [InlineData(0, false)]
    public void IsNotAvailableToCaller_OnlyForForbiddenAndNotFound(int status, bool expected)
    {
        var response = new ServiceAgentResponse(new ErrorResponse { Status = status }, null);

        Assert.Equal(expected, ExpandExternalServiceException.IsNotAvailableToCaller(response));
    }

    [Fact]
    public void IsNotAvailableToCaller_WithoutAnAnswer_IsFalse()
    {
        Assert.False(ExpandExternalServiceException.IsNotAvailableToCaller(new ServiceAgentResponse(null, new TimeoutException())));
    }
}
