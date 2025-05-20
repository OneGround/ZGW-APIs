using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Common.Web.Expands;

namespace OneGround.ZGW.Besluiten.Web.Expands.v1;

public class BesluitExpander : IObjectExpander<BesluitResponseDto>
{
    private readonly IExpanderFactory _expanderFactory;
    private HashSet<string> _expandLookup;

    public BesluitExpander(IExpanderFactory expanderFactory)
    {
        _expanderFactory = expanderFactory;
    }

    public string ExpandName => ExpanderNames.BesluitExpander;

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, BesluitResponseDto dto)
    {
        if (expandLookup.Count == 0)
        {
            return dto;
        }
        _expandLookup = expandLookup;
        var expandedObject = await ResolveChildrenParallelAsync(dto);

        if (expandedObject == null)
        {
            return dto;
        }

        var expandedDto = DtoExpander.Merge(dto, new { _expand = expandedObject });

        return expandedDto;
    }

    private async Task<object> ResolveChildrenParallelAsync(BesluitResponseDto dto)
    {
        var expandDictionary = new ExpandoObject() as IDictionary<string, object>;
        var resolveTasks = new Dictionary<string, Task<object>>();

        AddResolutionTask(resolveTasks, ExpandKeys.BesluitType, () => dto.BesluitType);
        AddResolutionTask(resolveTasks, ExpandKeys.BesluitInformatieObjecten, () => dto.Url);

        var resolvedResults = await Task.WhenAll(resolveTasks.Values);

        int index = 0;
        foreach (var key in resolveTasks.Keys)
        {
            expandDictionary[key] = resolvedResults[index++];
        }

        return expandDictionary.Any() ? expandDictionary : null;
    }

    private void AddResolutionTask<TEntity>(Dictionary<string, Task<object>> resolveTasks, string expandKey, Func<TEntity> paramFunc)
        where TEntity : class
    {
        if (_expandLookup.StartsOfAnyOf(expandKey))
        {
            var expander = _expanderFactory.Create<TEntity>(expandKey);
            resolveTasks.Add(expandKey, expander.ResolveAsync(_expandLookup, paramFunc()));
        }
    }
}
