using System.Collections.Generic;
using MediatR;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

// Note: the per-request caches (and the zaak lookup on top of one) that the expand resolvers get from DI, for tests that build a resolver by hand
internal static class ExpandCaches
{
    public static IZaakLookup ZaakLookup(IMediator mediator) => new ZaakLookup(mediator, new GenericCache<QueryResult<Zaak>>());

    public static IGenericCache<QueryResult<ZaakStatus>> Status() => new GenericCache<QueryResult<ZaakStatus>>();

    public static IGenericCache<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>> Document() =>
        new GenericCache<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>>();

    public static IGenericCache<ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>> ObjectInformatieObjecten() =>
        new GenericCache<ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>>();
}
