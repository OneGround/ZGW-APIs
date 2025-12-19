using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Documenten.ServiceAgent.v1._5;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._5;

public class ZaakInformatieObjectenExpander : IObjectExpander<string>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly IUserAuthDocumentenServiceAgent _documentenServiceAgent;
    private readonly IGenericCache<object> _informatieobjectResponseCache; // Note: The informatieobjecten comes from DRC (which had expanded repsonse as well)

    public ZaakInformatieObjectenExpander(
        IServiceProvider serviceProvider,
        IMapper mapper,
        IUserAuthDocumentenServiceAgent documentenServiceAgent,
        IGenericCache<object> informatieobjectResponseCache
    )
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
        _documentenServiceAgent = documentenServiceAgent;
        _informatieobjectResponseCache = informatieobjectResponseCache;
    }

    public string ExpandName => "zaakinformatieobjecten";

    public Task<object> ResolveAsync(HashSet<string> expandLookup, string zaak)
    {
        // Note: Not called directly so we can keep as it is now
        throw new NotImplementedException();
    }

    public async Task<object> ResolveAsync(IExpandParser expandLookup, string zaak)
    {
        object error = null;

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Get all zaak-informatieobjecten
        var result = await mediator.Send(
            new GetAllZaakInformatieObjectenQuery
            {
                GetAllZaakInformatieObjectenFilter = new Models.v1.GetAllZaakInformatieObjectenFilter { Zaak = zaak },
            }
        );

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

            // TODO: Should we validate the given pathes in expandLookup.Items?
            var zaakinformatieobjectLimited = JObjectFilter.FilterObjectByPaths(
                JObjectHelper.FromObjectOrDefault(zaakinformatieobjectDto),
                expandLookup.Items[ExpandName]
            );

            if (
                expandLookup.Expands.ContainsAnyOf(
                    "zaakinformatieobjecten.informatieobject",
                    "zaakinformatieobjecten.informatieobject.informatieobjecttype",
                    "zaakinformatieobjecten.informatieobject.informatieobjecttype.catalogus"
                )
            )
            {
                // Note: Set the requested expand to DRC (informatieobject)
                string expand = "";
                if (expandLookup.Expands.ContainsAnyOf("zaakinformatieobjecten.informatieobject.informatieobjecttype"))
                    expand = "informatieobjecttype";
                if (expandLookup.Expands.ContainsAnyOf("zaakinformatieobjecten.informatieobject.informatieobjecttype.catalogus"))
                    expand = "informatieobjecttype.catalogus";

                var informatieobjectResponse = await _informatieobjectResponseCache.GetOrCacheAndGetAsync(
                    $"key_{zaakinformatieobjectDto.InformatieObject}",
                    async () =>
                    {
                        var _informatieobjectResponse = await _documentenServiceAgent.GetEnkelvoudigInformatieObjectByUrlAsync(
                            zaakinformatieobjectDto.InformatieObject,
                            expand
                        );
                        return _informatieobjectResponse.Success
                            ? _informatieobjectResponse.Response.expandedEnkelvoudigInformatieObject
                            : ExpandError.Create(_informatieobjectResponse.Error);
                    }
                );

                // TODO: Moet dit niet limited worden door de DRC-API (voor nu even oke)
                var informatieobjectResponseLimited = JObjectFilter.FilterObjectByPaths(
                    JObjectHelper.FromObjectOrDefault(informatieobjectResponse),
                    expandLookup.Items[$"{ExpandName}.informatieobject"]
                );

                var zaakinformatieobjectDtoExpanded = DtoExpander.Merge(
                    zaakinformatieobjectLimited,
                    new { _expand = new { informatieobject = informatieobjectResponseLimited ?? new object() } }
                );
                zaakinformatieobjecten.Add(zaakinformatieobjectDtoExpanded);
            }
            else
            {
                zaakinformatieobjecten.Add(zaakinformatieobjectLimited);
            }
        }
        return zaakinformatieobjecten;
    }
}
