using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.ServiceAgent.Expands;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Documenten.Contracts.v1._7.Queries;
using OneGround.ZGW.Documenten.ServiceAgent.v1._7;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "zaakinformatieobjecten" expand path on a STATUS. Same shape as
/// <see cref="ZaakZaakInformatieObjectenResolver"/> (fresh DI scope per row for
/// GetAllZaakInformatieObjectenQuery's temp-table row-level authorization, cross-checked against
/// DRC's own authorization model via <see cref="IUserAuthDocumentenServiceAgent.GetObjectInformatieObjectenAsync"/>)
/// -- see that resolver's own remarks for why both of those are needed. Two differences: this filters
/// <see cref="OneGround.ZGW.Zaken.Web.Models.v1.GetAllZaakInformatieObjectenFilter.Status"/> by this
/// STATUS's own url (a new filter option added to that shared query/handler for this purpose --
/// Zaak/InformatieObject-based callers are unaffected), and the DRC cross-check is scoped to
/// <c>entity.Zaak</c> (the one ZAAK this STATUS belongs to). The request validators for creating/updating
/// a ZAAKINFORMATIEOBJECT don't actually enforce that its "status" belongs to the same ZAAK it's linked
/// to, so the filter also constrains on <c>Zaak = entity.Zaak</c> alongside <c>Status</c> -- this defends
/// against that data-model gap by simply excluding any (status, zaak) mismatch from the result, rather
/// than assuming it can't occur. Unlike the ZAAK-level resolver, there's no pre-populated url-count field
/// on STATUS, so this always queries ZRC first; it then skips the DRC call entirely when that query
/// comes back empty. "...informatieobject.informatieobjecttype" is forwarded to the same shared
/// <see cref="ExpandEngine{TEntity}"/> the ZAAK-level resolver uses -- see
/// <see cref="ZaakZaakInformatieObjectenResolver"/>'s own remarks for why a 4th nesting level isn't
/// offered (the VNG ZGW spec's 3-level expand cap).
/// </summary>
public class StatusZaakInformatieObjectenResolver : IExpandResolver<StatusResponseDto>
{
    private const string ServiceName = "DRC";
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly IUserAuthDocumentenServiceAgent _documentenServiceAgent;
    private readonly ExpandEngine<ZaakInformatieObjectResponseDto> _zaakInformatieObjectExpandEngine;

    public StatusZaakInformatieObjectenResolver(
        IServiceProvider serviceProvider,
        IMapper mapper,
        IUserAuthDocumentenServiceAgent documentenServiceAgent,
        ExpandEngine<ZaakInformatieObjectResponseDto> zaakInformatieObjectExpandEngine
    )
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
        _documentenServiceAgent = documentenServiceAgent;
        _zaakInformatieObjectExpandEngine = zaakInformatieObjectExpandEngine;
    }

    public string Path => "zaakinformatieobjecten";
    public string Parent => null;
    public IEnumerable<(string Path, string Parent)> AdditionalPaths =>
        [
            ("zaakinformatieobjecten.informatieobject", "zaakinformatieobjecten"),
            ("zaakinformatieobjecten.informatieobject.informatieobjecttype", "zaakinformatieobjecten.informatieobject"),
        ];

    public async Task<object> ResolveAsync(
        StatusResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new Handlers.v1._5.GetAllZaakInformatieObjectenQuery
            {
                GetAllZaakInformatieObjectenFilter = new Models.v1.GetAllZaakInformatieObjectenFilter { Status = entity.Url, Zaak = entity.Zaak },
            }
        );

        if (result.Result.Count == 0)
        {
            return new List<ZaakInformatieObjectResponseDto>();
        }

        var objectInformatieObjecten = await _documentenServiceAgent.GetObjectInformatieObjectenAsync(
            new GetAllObjectInformatieObjectenQueryParameters { Object = entity.Zaak }
        );

        if (!objectInformatieObjecten.Success)
        {
            throw new ExpandExternalServiceException(ServiceName, entity.Zaak, null);
        }

        var mirroredInformatieObjecten = objectInformatieObjecten.Response.Select(oio => oio.InformatieObject).ToHashSet();

        var zaakinformatieobjecten = result.Result.Where(zio => mirroredInformatieObjecten.Contains(zio.InformatieObject)).ToList();

        var response = _mapper.Map<List<ZaakInformatieObjectResponseDto>>(zaakinformatieobjecten);

        if (requestedPaths.Contains("zaakinformatieobjecten.informatieobject"))
        {
            List<string> nestedPaths = requestedPaths.Contains("zaakinformatieobjecten.informatieobject.informatieobjecttype")
                ? ["informatieobject", "informatieobject.informatieobjecttype"]
                : ["informatieobject"];

            await _zaakInformatieObjectExpandEngine.ResolveListAsync(response, nestedPaths);
        }

        return response;
    }
}
