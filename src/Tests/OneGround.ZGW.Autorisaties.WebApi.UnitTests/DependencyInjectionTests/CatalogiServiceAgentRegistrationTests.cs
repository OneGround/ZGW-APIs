using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Autorisaties.Common.BusinessRules;
using OneGround.ZGW.Autorisaties.Web;
using Xunit;

namespace OneGround.ZGW.Autorisaties.WebApi.UnitTests.DependencyInjectionTests;

/// <summary>
/// A Catalogi service agent that a class asks for but the host does not register only fails at runtime, on the first request that
/// reaches that class. This guards the registrations in <see cref="Startup"/> against the agents the host's own classes depend on.
/// </summary>
public class CatalogiServiceAgentRegistrationTests
{
    private const string CatalogiServiceAgentNamespace = "OneGround.ZGW.Catalogi.ServiceAgent";

    [Fact]
    public void EveryCatalogiServiceAgentTheHostDependsOn_IsRegistered()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["ConnectionStrings:UserConnectionString"] = "Host=localhost;Database=unused" })
            .Build();

        new Startup(configuration).ConfigureServices(services);

        var requiredAgents = typeof(ApplicatieBusinessRuleService)
            .Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetConstructors())
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType)
            .Where(t => t.IsInterface && t.Namespace != null && t.Namespace.StartsWith(CatalogiServiceAgentNamespace, StringComparison.Ordinal))
            .Distinct()
            .ToList();

        Assert.NotEmpty(requiredAgents); // Note: guards the check below, so it cannot pass because nothing was found

        var registered = services.Select(d => d.ServiceType).ToHashSet();

        Assert.Empty(requiredAgents.Where(agent => !registered.Contains(agent)).Select(a => a.FullName));
    }
}
