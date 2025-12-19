using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Documenten.Contracts.v1._5.Responses;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._5;

public class EnkelvoudigInformatieObjectExpander : IObjectExpander<EnkelvoudigInformatieObjectGetResponseDto>
{
    private readonly IExpanderFactory _expanderFactory;

    public EnkelvoudigInformatieObjectExpander(IExpanderFactory expanderFactory)
    {
        _expanderFactory = expanderFactory;
    }

    public string ExpandName => "enkelvoudiginformatieobject";

    public Task<object> ResolveAsync(IExpandParser expandLookup, EnkelvoudigInformatieObjectGetResponseDto entity)
    {
        // TODO: Implemement voor v1.6
        throw new NotImplementedException();
    }

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, EnkelvoudigInformatieObjectGetResponseDto enkelvoudigInformatieObjectDto)
    {
        if (expandLookup.Count != 0)
        {
            var expand = await ResolveChildrenParallelAsync(expandLookup, enkelvoudigInformatieObjectDto);
            if (expand != null)
            {
                var enkelvoudigInformatieObjectDtoExpanded = DtoExpander.Merge(enkelvoudigInformatieObjectDto, new { _expand = expand });

                return enkelvoudigInformatieObjectDtoExpanded;
            }
        }
        return enkelvoudigInformatieObjectDto;
    }

    private async Task<object> ResolveChildrenParallelAsync(
        HashSet<string> expandLookup,
        EnkelvoudigInformatieObjectGetResponseDto enkelvoudigInformatieObjectDto
    )
    {
        // Merge the enkelvoudiginformatieobject with sub-entities (expands)
        var result = new ExpandoObject();

        var tasks = new Dictionary<string, Task<object>>();

        IDictionary<string, object> expand = result;

        if (
            expandLookup.ContainsAnyOf(
                "informatieobject.informatieobjecttype",
                "informatieobject.informatieobjecttype.catalogus",
                "informatieobjecttype",
                "informatieobjecttype.catalogus"
            )
        )
        {
            var expander = _expanderFactory.Create<string>("informatieobjecttype");

            tasks.Add("informatieobjecttype", expander.ResolveAsync(expandLookup, enkelvoudigInformatieObjectDto.InformatieObjectType));
        }

        await Task.WhenAll(tasks.Values.ToArray());

        foreach (var _expand in tasks.Keys)
        {
            expand[_expand] = tasks[_expand].Result;
        }

        return expand.Any() ? result : null;
    }
}
