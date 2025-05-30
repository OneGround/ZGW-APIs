using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentValidation;

namespace Roxit.ZGW.Common.Web.Validations;

/// <summary>
/// Implements <see cref="AbstractValidator{T}"/> to set Cascade.Stop for each chained rule.
/// </summary>
public class ZGWValidator<T> : AbstractValidator<T>
{
    /// <summary>
    /// Defines a RuleSet that can be used to group together several validators.
    /// This automatically sets <see cref="CascadeMode.Stop"/> for each rule.
    /// </summary>
    public IRuleBuilderInitial<T, TProperty> CascadeRuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        return RuleFor(expression).Cascade(CascadeMode.Stop);
    }

    /// <summary>
    /// Defines a RuleSet that can be used to group together several validators.
    /// This automatically sets <see cref="CascadeMode.Stop"/> for each rule.
    /// </summary>
    public IRuleBuilderInitialCollection<T, TElement> CascadeRuleForEach<TElement>(Expression<Func<T, IEnumerable<TElement>>> expression)
    {
        return RuleForEach(expression).Cascade(CascadeMode.Stop).OverrideIndexer(ZGWIndex);
    }

    private static string ZGWIndex<TElement>(T model, IEnumerable<TElement> collection, TElement element, int index)
    {
        return $".{index}";
    }

    protected IEnumerable<string> TryList(string value)
    {
        if (string.IsNullOrEmpty(value))
            return [];

        return value.Split(",").Select(v => v.Trim());
    }
}
