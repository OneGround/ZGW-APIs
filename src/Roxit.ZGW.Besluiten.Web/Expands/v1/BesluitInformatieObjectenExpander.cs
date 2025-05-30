using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Roxit.ZGW.Besluiten.Contracts.v1.Responses;
using Roxit.ZGW.Besluiten.Web.Handlers.v1;
using Roxit.ZGW.Besluiten.Web.Models.v1;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Expands;
using Roxit.ZGW.Documenten.ServiceAgent.v1._5.Extensions;

namespace Roxit.ZGW.Besluiten.Web.Expands.v1;

public class BesluitInformatieObjectenExpander : IObjectExpander<string>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IUserAuthDocumentenServiceAgent _documentenServiceAgent;
    private readonly IGenericCache<object> _informatieobjectResponseCache;

    public BesluitInformatieObjectenExpander(
        IMediator mediator,
        IMapper mapper,
        IUserAuthDocumentenServiceAgent documentenServiceAgent,
        IGenericCache<object> informatieobjectResponseCache
    )
    {
        _mediator = mediator;
        _mapper = mapper;
        _documentenServiceAgent = documentenServiceAgent;
        _informatieobjectResponseCache = informatieobjectResponseCache;
    }

    public string ExpandName => ExpandKeys.BesluitInformatieObjecten;

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, string besluit)
    {
        object error = null;

        // Get all besluit-informatieobjecten
        var result = _mediator
            .Send(
                new GetAllBesluitInformatieObjectenQuery
                {
                    GetAllBesluitInformatieObjectenFilter = new GetAllBesluitInformatieObjectenFilter { Besluit = besluit },
                }
            )
            .Result;

        if (result.Status != QueryStatus.OK)
        {
            error = ExpandError.Create(result.Errors);
            return result.Errors;
        }

        // Only retrieve the mirrored besluit-document relationships that the Client(Id) have the rights to
        var objectinformatieobjecten = await _documentenServiceAgent.GetObjectInformatieObjectenAsync(
            new Documenten.Contracts.v1.Queries.GetAllObjectInformatieObjectenQueryParameters { Object = besluit }
        );

        if (!objectinformatieobjecten.Success)
        {
            error = ExpandError.Create(objectinformatieobjecten.Error);
            return error;
        }

        var lookupObjectInformatieobject = objectinformatieobjecten.Response.Select(oio => oio.InformatieObject).ToHashSet();

        var besluitInformatieObjecten = new List<object>();

        // Construct expand response with the mirrored besluit-document relationships that the Client(Id) have the rights to
        foreach (var besluitInformatieObject in result.Result.Where(zio => lookupObjectInformatieobject.Contains(zio.InformatieObject)))
        {
            var besluitInformatieObjectDto = _mapper.Map<BesluitInformatieObjectResponseDto>(besluitInformatieObject);

            if (
                expandLookup.ContainsAnyOf(
                    ExpandQueries.BesluitInformatieObjecten_InformatieObject,
                    ExpandQueries.BesluitInformatieObjecten_InformatieObject_InformatieObjectType
                )
            )
            {
                // Note: Set the requested expand to DRC (informatieobject)
                string expand = "";
                if (expandLookup.ContainsAnyOf(ExpandQueries.BesluitInformatieObjecten_InformatieObject_InformatieObjectType))
                {
                    expand = ExpandKeys.InformatieObjectType;
                }

                var informatieobjectResponse = _informatieobjectResponseCache.GetOrCacheAndGet(
                    $"key_{besluitInformatieObjectDto.InformatieObject}",
                    () =>
                    {
                        var _informatieobjectResponse = _documentenServiceAgent
                            .GetEnkelvoudigInformatieObjectByUrlAsync(besluitInformatieObjectDto.InformatieObject, expand)
                            .Result;
                        return _informatieobjectResponse.Success
                            ? _informatieobjectResponse.Response.expandedEnkelvoudigInformatieObject
                            : ExpandError.Create(_informatieobjectResponse.Error);
                    }
                );

                var zaakinformatieobjectDtoExpanded = DtoExpander.Merge(
                    besluitInformatieObjectDto,
                    new { _expand = new { informatieobject = informatieobjectResponse ?? error ?? new object() } }
                );
                besluitInformatieObjecten.Add(zaakinformatieobjectDtoExpanded);
            }
            else
            {
                besluitInformatieObjecten.Add(besluitInformatieObjectDto);
            }
        }
        return besluitInformatieObjecten;
    }
}
