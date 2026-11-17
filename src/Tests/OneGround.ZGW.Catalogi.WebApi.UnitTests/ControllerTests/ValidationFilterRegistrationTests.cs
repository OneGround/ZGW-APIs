using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Web.Controllers;
using OneGround.ZGW.Catalogi.Web.Extensions;
using OneGround.ZGW.Common.Web.Controllers;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.ControllerTests;

public class ValidationFilterRegistrationTests
{
    [Fact]
    public void ServiceFilters_Should_AllBeRegistered()
    {
        // A ServiceFilter whose type is not registered fails at runtime, so pin that every filter that the controllers use is registered
        var used = typeof(Api)
            .Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ZGWControllerBase).IsAssignableFrom(t))
            .SelectMany(c =>
                c.GetCustomAttributes<ServiceFilterAttribute>()
                    .Concat(
                        c.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                            .SelectMany(m => m.GetCustomAttributes<ServiceFilterAttribute>())
                    )
            )
            .Select(a => a.ServiceType)
            .Distinct()
            .ToList();

        var services = new ServiceCollection();
        services.AddQueryParameterValidations();
        var registered = services.Select(d => d.ServiceType).ToHashSet();

        Assert.NotEmpty(used);
        Assert.Empty(used.Where(t => !registered.Contains(t)).Select(t => t.ToString()));
    }
}
