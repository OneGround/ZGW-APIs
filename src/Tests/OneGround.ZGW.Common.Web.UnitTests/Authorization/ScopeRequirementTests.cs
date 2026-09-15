using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Common.Web.Authorization;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Authorization;

public class ScopeRequirementTests
{
    [Fact]
    public void An_action_carrying_a_scope_attribute_satisfies_the_requirement()
    {
        Assert.True(IsSatisfiedBy<PlainController>(nameof(PlainController.Scoped)));
    }

    [Fact]
    public void An_action_carrying_ScopeNotRequired_satisfies_the_requirement()
    {
        Assert.True(IsSatisfiedBy<PlainController>(nameof(PlainController.Exempt)));
    }

    [Fact]
    public void An_action_carrying_both_satisfies_the_requirement()
    {
        Assert.True(IsSatisfiedBy<PlainController>(nameof(PlainController.ScopedAndExempt)));
    }

    [Fact]
    public void An_action_carrying_neither_does_not_satisfy_the_requirement()
    {
        Assert.False(IsSatisfiedBy<PlainController>(nameof(PlainController.Bare)));
    }

    [Fact]
    public void A_class_level_exemption_covers_a_bare_action_on_that_controller()
    {
        Assert.True(IsSatisfiedBy<ExemptController>(nameof(ExemptController.Bare)));
    }

    [Fact]
    public void A_class_level_exemption_is_inherited_by_a_derived_controller()
    {
        Assert.True(IsSatisfiedBy<DerivedFromExemptController>(nameof(ExemptController.Bare)));
    }

    [Fact]
    public void ScopeNotRequired_demands_a_reason()
    {
        Assert.Throws<ArgumentException>(() => new ScopeNotRequiredAttribute(null));
        Assert.Throws<ArgumentException>(() => new ScopeNotRequiredAttribute(""));
        Assert.Throws<ArgumentException>(() => new ScopeNotRequiredAttribute("   "));

        Assert.Equal("because", new ScopeNotRequiredAttribute("because").Reason);
    }

    [Fact]
    public void Both_arguments_are_required()
    {
        var action = typeof(PlainController).GetMethod(nameof(PlainController.Bare));

        Assert.Throws<ArgumentNullException>(() => ScopeRequirement.IsSatisfiedBy(null, action));
        Assert.Throws<ArgumentNullException>(() => ScopeRequirement.IsSatisfiedBy(typeof(PlainController), null));
    }

    private static bool IsSatisfiedBy<TController>(string actionName)
    {
        var action = typeof(TController).GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(action);

        return ScopeRequirement.IsSatisfiedBy(typeof(TController), action);
    }

    private sealed class TestScopeAttribute : BaseScopeAttribute
    {
        public TestScopeAttribute()
            : base("TEST", "test.lezen") { }
    }

    private class PlainController : ControllerBase
    {
        [TestScope]
        public IActionResult Scoped() => Ok();

        [ScopeNotRequired("test")]
        public IActionResult Exempt() => Ok();

        [TestScope]
        [ScopeNotRequired("test")]
        public IActionResult ScopedAndExempt() => Ok();

        public IActionResult Bare() => Ok();
    }

    [ScopeNotRequired("test")]
    private class ExemptController : ControllerBase
    {
        public IActionResult Bare() => Ok();
    }

    private sealed class DerivedFromExemptController : ExemptController { }
}
