using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Documenten.ServiceAgent.v1._5.Extensions;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._5;

public class ZaakInformatieObjectenExpander : IObjectExpander<string>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IUserAuthDocumentenServiceAgent _documentenServiceAgent;
    private readonly IGenericCache<object> _informatieobjectResponseCache; // Note: The informatieobjecten comes from DRC (which had expanded repsonse as well)

    public ZaakInformatieObjectenExpander(
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

    public string ExpandName => "zaakinformatieobjecten";

    public async Task<object> ResolveAsync(HashSet<string> expandLookup, string zaak)
    {
        object error = null;

        // Get all zaak-informatieobjecten
        var result = _mediator
            .Send(
                new GetAllZaakInformatieObjectenQuery
                {
                    GetAllZaakInformatieObjectenFilter = new Models.v1.GetAllZaakInformatieObjectenFilter { Zaak = zaak },
                }
            )
            .Result;

        if (result.Status != QueryStatus.OK)
        {
            error = ExpandError.Create(result.Errors);
            return error;
        }

        // Only retrieve the mirrored zaak-document relationships that the Client(Id) have the rights to
        var objectinformatieobjecten = await _documentenServiceAgent.GetObjectInformatieObjectenAsync(
            new Documenten.Contracts.v1.Queries.GetAllObjectInformatieObjectenQueryParameters { Object = zaak }
        );

        if (!objectinformatieobjecten.Success)
        {
            error = ExpandError.Create(objectinformatieobjecten.Error);
            return error;
        }

        var lookupObjectInformatieobject = objectinformatieobjecten.Response.Select(oio => oio.InformatieObject).ToHashSet();

        var zaakinformatieobjecten = new List<object>();

        // Construct expand response with the mirrored zaak-document relationships that the Client(Id) have the rights to
        foreach (var zaakinformatieobject in result.Result.Where(zio => lookupObjectInformatieobject.Contains(zio.InformatieObject)))
        {
            var zaakinformatieobjectDto = _mapper.Map<ZaakInformatieObjectResponseDto>(zaakinformatieobject);

            if (
                expandLookup.ContainsAnyOf(
                    "zaakinformatieobjecten.informatieobject",
                    "zaakinformatieobjecten.informatieobject.informatieobjecttype",
                    "zaakinformatieobjecten.informatieobject.informatieobjecttype.catalogus"
                )
            )
            {
                // Note: Set the requested expand to DRC (informatieobject)
                string expand = "";
                if (expandLookup.ContainsAnyOf("zaakinformatieobjecten.informatieobject.informatieobjecttype"))
                    expand = "informatieobjecttype";
                if (expandLookup.ContainsAnyOf("zaakinformatieobjecten.informatieobject.informatieobjecttype.catalogus"))
                    expand = "informatieobjecttype.catalogus";

                var informatieobjectResponse = _informatieobjectResponseCache.GetOrCacheAndGet(
                    $"key_{zaakinformatieobjectDto.InformatieObject}",
                    () =>
                    {
                        var _informatieobjectResponse = _documentenServiceAgent
                            .GetEnkelvoudigInformatieObjectByUrlAsync(zaakinformatieobjectDto.InformatieObject, expand)
                            .Result;
                        if (!_informatieobjectResponse.Success)
                        {
                            error = ExpandError.Create(_informatieobjectResponse.Error);
                            return null;
                        }
                        return _informatieobjectResponse.Response.expandedEnkelvoudigInformatieObject;
                    }
                );

                var zaakinformatieobjectDtoExpanded = DtoExpander.Merge(
                    zaakinformatieobjectDto,
                    new { _expand = new { informatieobject = informatieobjectResponse ?? error ?? new object() } }
                );
                zaakinformatieobjecten.Add(zaakinformatieobjectDtoExpanded);
            }
            else
            {
                zaakinformatieobjecten.Add(zaakinformatieobjectDto);
            }
        }
        return zaakinformatieobjecten;
    }
}
