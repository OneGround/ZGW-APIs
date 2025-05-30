using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using FluentValidation;

namespace Roxit.ZGW.Common.Web.Validations;

public static class AbstractValidatorExtensions
{
    /// <summary>
    /// Defines a RuleSet that can be used to group together several validators.
    /// This automatically sets <see cref="CascadeMode.Stop"/> for each rule.
    /// </summary>
    public static IRuleBuilderInitial<T, TProperty> CascadeRuleFor<T, TProperty>(
        this InlineValidator<T> inlineValidator,
        Expression<Func<T, TProperty>> expression
    )
    {
        return inlineValidator.RuleFor(expression).Cascade(CascadeMode.Stop);
    }

    /// <summary>
    /// Invokes a rule for each item in the collection.
    /// This automatically sets <see cref="CascadeMode.Stop"/> for each rule.
    /// </summary>
    public static IRuleBuilderInitialCollection<T, TElement> CascadeRuleForEach<T, TElement>(
        this InlineValidator<T> inlineValidator,
        Expression<Func<T, IEnumerable<TElement>>> expression
    )
    {
        return inlineValidator.RuleForEach(expression).Cascade(CascadeMode.Stop);
    }
}
