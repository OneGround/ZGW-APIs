using OneGround.ZGW.Common.Handlers;

namespace OneGround.ZGW.Common.Web.Expands;

/// <summary>
/// Decides what an expand resolver does with the status of the (same-service) query that fetched the entity it has to expand. Shared by
/// the resolvers of every service that expands an entity of its own through MediatR.
/// <para>
/// NotFound and Forbidden mean the referenced entity is not available to the caller: it does not exist, or the caller may not see it. Such a
/// reference is left out of the expand (the resolver resolves to null, which becomes an empty object, or to an empty list) instead of
/// failing the whole request -- the same outcome as an entry that a list resolver (deelzaken, relevanteanderezaken, ...) leaves out because
/// the authorization join does not return it. It also means an expand cannot be used to find out whether an entity exists (403 versus 404).
/// </para>
/// <para>
/// Any other non-OK status is a real failure: it throws <see cref="ExpandInternalQueryHandlerException"/>, which the controllers translate
/// (InterneQueryHandlerFout).
/// </para>
/// </summary>
public static class ExpandQueryStatus
{
    public static bool IsAvailable(QueryStatus status, string resource) =>
        status switch
        {
            QueryStatus.OK => true,
            QueryStatus.NotFound or QueryStatus.Forbidden => false,
            _ => throw new ExpandInternalQueryHandlerException(resource, status),
        };
}
