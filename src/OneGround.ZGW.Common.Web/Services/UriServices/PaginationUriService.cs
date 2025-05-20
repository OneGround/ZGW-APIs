using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Contracts.Extensions;

namespace OneGround.ZGW.Common.Web.Services.UriServices;

public interface IPaginationUriService
{
    Uri GetUri(IQueryParameters query, int page, int? size = null);
    Uri GetUri(int page);
}

public class PaginationUriService : BaseUriService, IPaginationUriService
{
    public PaginationUriService(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor) { }

    public Uri GetUri(int page)
    {
        var builder = new UriBuilder(BaseUri) { Path = Path };

        string modifiedUri = builder.Uri.ToString();

        modifiedUri = QueryHelpers.AddQueryString(modifiedUri, "page", page.ToString());

        return new Uri(modifiedUri);
    }

    public Uri GetUri(IQueryParameters query, int page, int? size = null)
    {
        var builder = new UriBuilder(BaseUri) { Path = Path };

        string modifiedUri = builder.Uri.ToString();
        if (query != null)
        {
            foreach (var queryParameter in query.GetParameters())
            {
                var value = queryParameter.GetValue(query);
                if (value != null)
                    modifiedUri = QueryHelpers.AddQueryString(modifiedUri, queryParameter.QueryName, value);
            }
        }

        modifiedUri = QueryHelpers.AddQueryString(modifiedUri, "page", page.ToString());

        if (size.HasValue)
        {
            modifiedUri = QueryHelpers.AddQueryString(modifiedUri, "size", size.Value.ToString());
        }

        return new Uri(modifiedUri);
    }
}
