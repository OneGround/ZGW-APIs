using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using OneGround.ZGW.Common.Web.Handlers;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests;

public class RetrieveAuditClientExclusionTests
{
    private static IConfiguration BuildConfig(params string[] excludeClientIds)
    {
        var dict = new Dictionary<string, string>();
        for (var i = 0; i < excludeClientIds.Length; i++)
            dict[$"Application:AudittrailRetrieveRecordExcludeClientIds:{i}"] = excludeClientIds[i];

        return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
    }

    private static IHttpContextAccessor HttpContextWithClientId(string clientId)
    {
        var claims = clientId == null ? Array.Empty<Claim>() : new[] { new Claim("client_id", clientId) };
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);
        return accessor.Object;
    }

    [Fact]
    public void Excluded_when_client_id_matches_glob()
    {
        var sut = new RetrieveAuditClientExclusion(BuildConfig("acme.tool-*"), HttpContextWithClientId("acme.tool-000"));
        Assert.True(sut.IsCurrentClientExcluded);
    }

    [Fact]
    public void Not_excluded_when_client_id_does_not_match()
    {
        var sut = new RetrieveAuditClientExclusion(BuildConfig("acme.tool-*"), HttpContextWithClientId("municipality-client-1"));
        Assert.False(sut.IsCurrentClientExcluded);
    }

    [Fact]
    public void Not_excluded_when_exclude_list_empty()
    {
        var sut = new RetrieveAuditClientExclusion(BuildConfig(), HttpContextWithClientId("acme.tool-000"));
        Assert.False(sut.IsCurrentClientExcluded);
    }

    [Fact]
    public void Not_excluded_when_client_id_missing()
    {
        var sut = new RetrieveAuditClientExclusion(BuildConfig("acme.tool-*"), HttpContextWithClientId(null));
        Assert.False(sut.IsCurrentClientExcluded);
    }
}
