using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>
/// Resolves the "resultaattypen" expand path of a ZaakType: the ResultaatTypen it refers to by URL. It also serves the paths below it
/// ("resultaattypen.selectielijstklasse" and "resultaattypen.resultaattypeomschrijving"): the engine cannot expand into a list, so for each
/// ResultaatType the paths that are requested are resolved with the engine of the ResultaatType itself (concurrently: they mostly wait for external documents, which the client of the request fetches once per URL).
/// </summary>
public class ZaakType_ResultaatTypen_Resolver
    : ChildTypen_ResolverBase<ZaakTypeResponseDto, ResultaatType, ResultaatTypeResponseDto>,
        // Note: listed again so that the interface is re-implemented and AdditionalPaths is seen by the engine and the validator
        IExpandResolver<ZaakTypeResponseDto>
{
    // Note: the expand paths of a ResultaatType that can be requested below "resultaattypen"; each is the path in the ResultaatType engine
    private static readonly string[] ChildPaths = ["selectielijstklasse", "resultaattypeomschrijving"];

    private readonly IServiceProvider _serviceProvider;

    public ZaakType_ResultaatTypen_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
        : base(serviceProvider, uriService)
    {
        _serviceProvider = serviceProvider;
    }

    public override string Path => "resultaattypen";

    public IEnumerable<(string Path, string Parent)> AdditionalPaths => ChildPaths.Select(c => ($"{Path}.{c}", Path));

    protected override string Resource => "resultaattype";

    protected override IEnumerable<string> GetUrls(ZaakTypeResponseDto zaakType) => zaakType.ResultaatTypen;

    protected override Task<QueryResult<ResultaatType>> GetAsync(IMediator mediator, Guid id) => mediator.Send(new GetResultaatTypeQuery { Id = id });

    public override async Task<object> ResolveAsync(
        ZaakTypeResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        var resultaatTypen = await LoadTypesAsync(GetUrls(entity));

        var childPaths = ChildPaths.Where(c => requestedPaths.Contains($"{Path}.{c}")).ToList();
        if (childPaths.Count == 0)
        {
            return resultaatTypen;
        }

        // Note: the engine is only needed (and made) when a child path is requested
        var resultaatTypeEngine = _serviceProvider.GetRequiredService<ExpandEngine<ResultaatTypeResponseDto>>();

        // Note: a ResultaatType is shared by all the ZaakTypen of a list that refer to it (per-request cache) and the requested paths are the same for
        // all of them, so one that is already expanded is left as is. The ResultaatTypen are expanded concurrently: the work is waiting for external
        // documents, so a zaaktype with many resultaattypen would otherwise use up the time budget of the request one call after the other.
        await Task.WhenAll(
            resultaatTypen
                .Where(r => !childPaths.All(c => r.Expand?.ContainsKey(c) == true))
                .Select(resultaatType => resultaatTypeEngine.ResolveAsync(resultaatType, childPaths))
        );

        return resultaatTypen;
    }
}
