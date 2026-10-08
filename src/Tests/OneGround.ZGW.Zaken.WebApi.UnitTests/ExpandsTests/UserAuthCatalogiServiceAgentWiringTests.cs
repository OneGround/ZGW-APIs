using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Extensions;
using OneGround.ZGW.Common.ServiceAgent.Caching;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

/// <summary>
/// The v1.7 expands must reach ZTC as the calling client, and must never share a response cache between callers (the cache is keyed
/// by RSIN and url, not by user).
/// </summary>
public class UserAuthCatalogiServiceAgentWiringTests
{
    private static IServiceCollection Register(bool serviceAccountAgentToo = false)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        if (serviceAccountAgentToo)
            services.AddCatalogiServiceAgent_v1_3(configuration);

        services.AddUserAuthCatalogiServiceAgent_v1_3(configuration);

        return services;
    }

    [Fact]
    public void Decorator_WrapsTheUserAuthenticatedAgent_NotTheServiceAccountAgent()
    {
        var constructor = Assert.Single(typeof(CatalogiServiceAgentDecorator).GetConstructors());

        var parameter = Assert.Single(constructor.GetParameters());
        Assert.Equal(typeof(IUserAuthCatalogiServiceAgent), parameter.ParameterType);
    }

    [Fact]
    public void UserAuthAgent_HasNoResponseCache()
    {
        var services = Register();

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(CachingConfiguration<IUserAuthCatalogiServiceAgent>));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(CachingHandler<IUserAuthCatalogiServiceAgent>));
    }

    [Fact]
    public void ServiceAccountAgent_StillHasItsResponseCache()
    {
        // Note: guards the test above -- the cache registration it looks for really exists for the service-account agent
        var services = Register(serviceAccountAgentToo: true);

        Assert.Single(services.Where(d => d.ServiceType == typeof(CachingConfiguration<ICatalogiServiceAgent>)));
    }

    [Fact]
    public void UserAuthAgent_IsRegisteredAsAnAgentOfItsOwn()
    {
        var services = Register(serviceAccountAgentToo: true);

        Assert.Contains(services, d => d.ServiceType == typeof(IUserAuthCatalogiServiceAgent));
        Assert.Contains(services, d => d.ServiceType == typeof(ICatalogiServiceAgent));
    }
}
