using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._5;

public class ZaakExpander : IObjectExpander<ZaakResponseDto>
{
    private readonly IExpanderFactory _expanderFactory;

    public ZaakExpander(IExpanderFactory expanderFactory)
    {
        _expanderFactory = expanderFactory;
    }

    public string ExpandName => "zaak";

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, ZaakResponseDto zaakDto)
    {
        var expands = new LegacyExpandAdapter(expandLookup, ExpandName);

        return await ResolveAsync(expands, zaakDto);
    }

    public async Task<object> ResolveAsync(IExpandParser expandLookup, ZaakResponseDto zaakDto)
    {
        if (expandLookup.Expands.Count != 0)
        {
            var expand = await ResolveChildrenParallelAsync(expandLookup, zaakDto);
            if (expand != null)
            {
                var zaakDtoExpanded = DtoExpander.Merge(zaakDto, new { _expand = expand });

                return zaakDtoExpanded;
            }
        }
        return zaakDto;
    }

    private async Task<object> ResolveChildrenParallelAsync(IExpandParser expandLookup, ZaakResponseDto zaakDto)
    {
        // Merge the zaak with sub-entities (expands)
        var result = new ExpandoObject();

        var tasks = new Dictionary<string, Task<object>>();

        IDictionary<string, object> expand = result;

        if (expandLookup.Expands.StartsOfAnyOf("zaaktype"))
        {
            var expander = _expanderFactory.Create<string>("zaaktype");

            tasks.Add("zaaktype", expander.ResolveAsync(expandLookup, zaakDto.Zaaktype));
        }
        if (expandLookup.Expands.StartsOfAnyOf("status"))
        {
            var expander = _expanderFactory.Create<string>("status");

            tasks.Add("status", expander.ResolveAsync(expandLookup, zaakDto.Url));
        }
        if (expandLookup.Expands.StartsOfAnyOf("resultaat"))
        {
            var expander = _expanderFactory.Create<string>("resultaat");

            tasks.Add("resultaat", expander.ResolveAsync(expandLookup, zaakDto.Url));
        }

        if (expandLookup.Expands.StartsOfAnyOf("hoofdzaak"))
        {
            var expander = _expanderFactory.Create<string>("hoofdzaak");

            tasks.Add("hoofdzaak", expander.ResolveAsync(expandLookup, zaakDto.Hoofdzaak));
        }

        if (expandLookup.Expands.StartsOfAnyOf("relevanteanderezaken"))
        {
            var expander = _expanderFactory.Create<IEnumerable<string>>("relevanteanderezaken");

            tasks.Add("relevanteanderezaken", expander.ResolveAsync(expandLookup, zaakDto.RelevanteAndereZaken.Select(z => z.Url)));
        }

        if (expandLookup.Expands.StartsOfAnyOf("deelzaken"))
        {
            var expander = _expanderFactory.Create<IEnumerable<string>>("deelzaken");

            tasks.Add("deelzaken", expander.ResolveAsync(expandLookup, zaakDto.Deelzaken));
        }

        if (expandLookup.Expands.StartsOfAnyOf("zaakinformatieobjecten"))
        {
            var expander = _expanderFactory.Create<string>("zaakinformatieobjecten");

            tasks.Add("zaakinformatieobjecten", expander.ResolveAsync(expandLookup, zaakDto.Url));
        }

        if (expandLookup.Expands.StartsOfAnyOf("eigenschappen"))
        {
            var expander = _expanderFactory.Create<string>("eigenschappen");

            tasks.Add("eigenschappen", expander.ResolveAsync(expandLookup, zaakDto.Url));
        }

        if (expandLookup.Expands.StartsOfAnyOf("rollen"))
        {
            var expander = _expanderFactory.Create<string>("rollen");

            tasks.Add("rollen", expander.ResolveAsync(expandLookup, zaakDto.Url));
        }

        if (expandLookup.Expands.StartsOfAnyOf("zaakobjecten"))
        {
            var expander = _expanderFactory.Create<string>("zaakobjecten");

            tasks.Add("zaakobjecten", expander.ResolveAsync(expandLookup, zaakDto.Url));
        }

        if (expandLookup.Expands.StartsOfAnyOf("zaakverzoeken"))
        {
            var expander = _expanderFactory.Create<string>("zaakverzoeken");

            tasks.Add("zaakverzoeken", expander.ResolveAsync(expandLookup, zaakDto.Url));
        }

        if (expandLookup.Expands.StartsOfAnyOf("zaakcontactmomenten"))
        {
            var expander = _expanderFactory.Create<string>("zaakcontactmomenten");

            tasks.Add("zaakcontactmomenten", expander.ResolveAsync(expandLookup, zaakDto.Url));
        }

        await Task.WhenAll(tasks.Values.ToArray());

        foreach (var _expand in tasks.Keys)
        {
            expand[_expand] = tasks[_expand].Result;
        }

        return expand.Any() ? result : null;
    }
}
