using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>
/// Resolves the "informatieobjecttype" expand path of a ZaakTypeInformatieObjectType. Unlike the other expands this is no reference by URL: the response
/// holds the omschrijving of the InformatieObjectType, and the relation is a soft one (the InformatieObjectType of the same catalogus with that
/// omschrijving that is valid today, as the zaaktype resolves its informatieobjecttypen, see ZaakTypeDataService). It is looked up with the same query
/// as the list of informatieobjecttypen, which applies that rule. Several versions can be valid today (the zaaktype lists all of them), but the expand is
/// one object: the newest one, the one with the latest begin of its geldigheid. Without an omschrijving or a catalogus, or without a valid
/// InformatieObjectType, the expand is an empty object.
/// </summary>
public class ZaakTypeInformatieObjectType_InformatieObjectType_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : IExpandResolver<ZaakTypeInformatieObjectTypeResponseDto>
{
    // Note: More than the versions of one omschrijving that can be valid at the same time
    private const int MaxVersions = 100;

    public string Path => "informatieobjecttype";
    public string Parent => null;

    public async Task<object> ResolveAsync(
        ZaakTypeInformatieObjectTypeResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        var omschrijving = entity.InformatieObjectType;
        if (string.IsNullOrWhiteSpace(omschrijving) || string.IsNullOrWhiteSpace(entity.Catalogus))
        {
            return null;
        }

        // Note: The cache of the request, shared with the other resolvers of an InformatieObjectType. The key can never be the id of an InformatieObjectType.
        var cache = serviceProvider.GetRequiredService<IGenericCache<InformatieObjectTypeResponseDto>>();
        var key = $"{uriService.GetId(entity.Catalogus)}:{omschrijving}";

        return await cache.GetOrCacheAndGetAsync(key, () => GetInformatieObjectTypeAsync(entity.Catalogus, omschrijving));
    }

    private async Task<InformatieObjectTypeResponseDto> GetInformatieObjectTypeAsync(string catalogusUrl, string omschrijving)
    {
        // Note: A scope of its own (so a DbContext of its own), only made when the InformatieObjectType is not in the cache yet
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = await mediator.Send(
            new GetAllInformatieObjectTypenQuery
            {
                // Note: The status is definitief and the date is today by default, like the zaaktype resolves its soft relations
                GetAllInformatieObjectTypenFilter = new Models.v1.GetAllInformatieObjectTypenFilter
                {
                    Catalogus = catalogusUrl,
                    Omschrijving = omschrijving,
                    DatumGeldigheid = DateOnly.FromDateTime(DateTime.Today),
                },
                Pagination = new PaginationFilter { Page = 1, Size = MaxVersions },
            }
        );
        if (!ExpandQueryStatus.IsAvailable(result.Status, "informatieobjecttype"))
        {
            return null;
        }

        var newest = result.Result.PageResult.OrderByDescending(i => i.BeginGeldigheid).FirstOrDefault();

        return newest is null ? null : mapper.Map<InformatieObjectTypeResponseDto>(newest);
    }
}
