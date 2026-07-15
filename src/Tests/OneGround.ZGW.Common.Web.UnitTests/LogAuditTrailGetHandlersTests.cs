using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.AuditTrail;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests;

public class LogAuditTrailGetHandlersTests
{
    private static IConfiguration BuildConfig(bool minimal, params string[] excludeClientIds)
    {
        var dict = new Dictionary<string, string> { ["Application:AudittrailRecordRetrieveMinimal"] = minimal.ToString() };
        for (var i = 0; i < excludeClientIds.Length; i++)
            dict[$"Application:AudittrailRetrieveRecordExcludeClientIds:{i}"] = excludeClientIds[i];

        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static IHttpContextAccessor HttpContextWithClientId(string clientId)
    {
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("client_id", clientId) })) };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);
        return accessor.Object;
    }

    private static IRetrieveAuditClientExclusion Exclusion(IConfiguration config, string clientId)
    {
        return new RetrieveAuditClientExclusion(config, HttpContextWithClientId(clientId));
    }

    private static IAuthorizationContextAccessor AuthContextAccessor()
    {
        var accessor = new Mock<IAuthorizationContextAccessor>();
        accessor
            .Setup(a => a.AuthorizationContext)
            .Returns(new AuthorizationContext(new AuthorizedApplication { Rsin = "rsin-test" }, Array.Empty<string>()));
        return accessor.Object;
    }

    private static (Mock<IAuditTrailFactory> factory, IDbContextWithAuditTrail context) AuditTrailDeps()
    {
        var service = new Mock<IAuditTrailService>();
        service
            .Setup(s => s.GetAsync(It.IsAny<IBaseEntity>(), It.IsAny<IUrlEntity>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        service.Setup(s => s.GetListAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var factory = new Mock<IAuditTrailFactory>();
        factory.Setup(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>())).Returns(service.Object);

        var context = new Mock<IDbContextWithAuditTrail>();
        context.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        return (factory, context.Object);
    }

    // ---- single-object GET ----

    [Fact]
    public async Task Object_retrieve_is_recorded_for_non_excluded_client()
    {
        var (factory, context) = AuditTrailDeps();
        var config = BuildConfig(minimal: true, "acme.tool-*");
        var handler = new LogAuditTrailGetObjectCommandHandler(
            config,
            context,
            factory.Object,
            AuthContextAccessor(),
            Exclusion(config, "municipality-client-1")
        );

        await handler.Handle(new LogAuditTrailGetObjectCommand { RetrieveCatagory = RetrieveCatagory.Minimal }, CancellationToken.None);

        factory.Verify(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>()), Times.Once());
    }

    [Fact]
    public async Task Object_retrieve_is_skipped_for_excluded_client()
    {
        var (factory, context) = AuditTrailDeps();
        var config = BuildConfig(minimal: true, "acme.tool-*");
        var handler = new LogAuditTrailGetObjectCommandHandler(
            config,
            context,
            factory.Object,
            AuthContextAccessor(),
            Exclusion(config, "acme.tool-000")
        );

        await handler.Handle(new LogAuditTrailGetObjectCommand { RetrieveCatagory = RetrieveCatagory.Minimal }, CancellationToken.None);

        factory.Verify(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>()), Times.Never());
    }

    [Fact]
    public async Task Object_retrieve_All_category_is_skipped_when_minimal_config_is_true()
    {
        var (factory, context) = AuditTrailDeps();
        var config = BuildConfig(minimal: true);
        var handler = new LogAuditTrailGetObjectCommandHandler(
            config,
            context,
            factory.Object,
            AuthContextAccessor(),
            Exclusion(config, "municipality-client-1")
        );

        await handler.Handle(new LogAuditTrailGetObjectCommand { RetrieveCatagory = RetrieveCatagory.All }, CancellationToken.None);

        factory.Verify(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>()), Times.Never());
    }

    [Fact]
    public async Task Object_retrieve_All_category_is_recorded_when_minimal_config_is_false()
    {
        var (factory, context) = AuditTrailDeps();
        var config = BuildConfig(minimal: false);
        var handler = new LogAuditTrailGetObjectCommandHandler(
            config,
            context,
            factory.Object,
            AuthContextAccessor(),
            Exclusion(config, "municipality-client-1")
        );

        await handler.Handle(new LogAuditTrailGetObjectCommand { RetrieveCatagory = RetrieveCatagory.All }, CancellationToken.None);

        factory.Verify(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>()), Times.Once());
    }

    // ---- list GET (previously gated off; now follows the same rule) ----

    [Fact]
    public async Task List_retrieve_is_recorded_for_non_excluded_client()
    {
        var (factory, context) = AuditTrailDeps();
        var config = BuildConfig(minimal: true, "acme.tool-*");
        var handler = new LogAuditTrailGetObjectListCommandHandler(
            config,
            context,
            factory.Object,
            AuthContextAccessor(),
            Exclusion(config, "municipality-client-1")
        );

        await handler.Handle(
            new LogAuditTrailGetObjectListCommand { RetrieveCatagory = RetrieveCatagory.Minimal, TotalCount = 5 },
            CancellationToken.None
        );

        factory.Verify(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>()), Times.Once());
    }

    [Fact]
    public async Task List_retrieve_is_skipped_for_excluded_client()
    {
        var (factory, context) = AuditTrailDeps();
        var config = BuildConfig(minimal: true, "acme.tool-*");
        var handler = new LogAuditTrailGetObjectListCommandHandler(
            config,
            context,
            factory.Object,
            AuthContextAccessor(),
            Exclusion(config, "acme.tool-000")
        );

        await handler.Handle(
            new LogAuditTrailGetObjectListCommand { RetrieveCatagory = RetrieveCatagory.Minimal, TotalCount = 5 },
            CancellationToken.None
        );

        factory.Verify(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>()), Times.Never());
    }

    [Fact]
    public async Task List_retrieve_All_category_is_skipped_when_minimal_config_is_true()
    {
        var (factory, context) = AuditTrailDeps();
        var config = BuildConfig(minimal: true);
        var handler = new LogAuditTrailGetObjectListCommandHandler(
            config,
            context,
            factory.Object,
            AuthContextAccessor(),
            Exclusion(config, "municipality-client-1")
        );

        await handler.Handle(
            new LogAuditTrailGetObjectListCommand { RetrieveCatagory = RetrieveCatagory.All, TotalCount = 5 },
            CancellationToken.None
        );

        factory.Verify(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>()), Times.Never());
    }

    [Fact]
    public async Task List_retrieve_All_category_is_recorded_when_minimal_config_is_false()
    {
        var (factory, context) = AuditTrailDeps();
        var config = BuildConfig(minimal: false);
        var handler = new LogAuditTrailGetObjectListCommandHandler(
            config,
            context,
            factory.Object,
            AuthContextAccessor(),
            Exclusion(config, "municipality-client-1")
        );

        await handler.Handle(
            new LogAuditTrailGetObjectListCommand { RetrieveCatagory = RetrieveCatagory.All, TotalCount = 5 },
            CancellationToken.None
        );

        factory.Verify(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>()), Times.Once());
    }
}
