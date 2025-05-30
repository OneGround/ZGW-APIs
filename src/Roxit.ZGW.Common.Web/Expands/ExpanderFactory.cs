using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Roxit.ZGW.Common.Web.Expands;

public class ExpanderFactory : IExpanderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ExpanderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IObjectExpander<TEntity> Create<TEntity>(string expandName)
        where TEntity : class
    {
        var expander = _serviceProvider
            .GetService<IEnumerable<IObjectExpander<TEntity>>>()
            .OfType<IObjectExpander<TEntity>>()
            .SingleOrDefault(e => e.ExpandName == expandName);

        return expander
            ?? throw new InvalidOperationException($"Could not find ObjectExpander of type {typeof(TEntity)} with registered name {expandName}.");
    }
}

public interface IExpanderFactory
{
    IObjectExpander<TEntity> Create<TEntity>(string expandName)
        where TEntity : class;
}
