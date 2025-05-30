using System;
using System.Collections.Generic;
using System.Linq;
using Roxit.ZGW.Common.Contracts;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Common.Web.Services;

public interface IPaginationHelper
{
    bool ValidatePaginatedResponse(PaginationFilter pagination, int totalCount);
    PagedResponse<TResponse> CreatePaginatedResponse<TResponse>(
        IQueryParameters query,
        PaginationFilter pagination,
        IEnumerable<TResponse> response,
        int totalCount,
        bool showSize = false
    );
    PagedResponse<TResponse> CreatePaginatedResponse<TResponse>(PaginationFilter pagination, IEnumerable<TResponse> response, int totalCount);
}

public class PaginationHelper : IPaginationHelper
{
    private readonly IPaginationUriService _paginationUriService;

    public PaginationHelper(IPaginationUriService paginationUriService)
    {
        _paginationUriService = paginationUriService;
    }

    public bool ValidatePaginatedResponse(PaginationFilter pagination, int totalCount)
    {
        bool isValid = (totalCount == 0 && pagination.Page == 1) || !(pagination.Size * (pagination.Page - 1) >= totalCount);

        return isValid;
    }

    public PagedResponse<TResponse> CreatePaginatedResponse<TResponse>(PaginationFilter pagination, IEnumerable<TResponse> response, int totalCount)
    {
        var nextPage =
            pagination.Page >= 1 && (pagination.Page * pagination.Size) < totalCount
                ? GetAbsoluteUri(_paginationUriService.GetUri(pagination.Page + 1))
                : null;

        var previousPage =
            pagination.Page - 1 >= 1 && ((pagination.Page - 1) * pagination.Size) < totalCount
                ? GetAbsoluteUri(_paginationUriService.GetUri(pagination.Page - 1))
                : null;

        var materializedResponse = response.ToList();

        return new PagedResponse<TResponse>
        {
            Results = materializedResponse,
            Count = totalCount,
            Next = materializedResponse.Count != 0 ? nextPage : null,
            Previous = previousPage,
        };
    }

    public PagedResponse<TResponse> CreatePaginatedResponse<TResponse>(
        IQueryParameters query,
        PaginationFilter pagination,
        IEnumerable<TResponse> response,
        int totalCount,
        bool showSize = false
    )
    {
        var nextPage =
            pagination.Page >= 1 && (pagination.Page * pagination.Size) < totalCount
                ? GetAbsoluteUri(_paginationUriService.GetUri(query, pagination.Page + 1, showSize ? pagination.Size : null))
                : null;

        var previousPage =
            pagination.Page - 1 >= 1 && ((pagination.Page - 1) * pagination.Size) < totalCount
                ? GetAbsoluteUri(_paginationUriService.GetUri(query, pagination.Page - 1, showSize ? pagination.Size : null))
                : null;

        var materializedResponse = response.ToList();

        return new PagedResponse<TResponse>
        {
            Results = materializedResponse,
            Count = totalCount,
            Next = materializedResponse.Count != 0 ? nextPage : null,
            Previous = previousPage,
        };
    }

    private static string GetAbsoluteUri(Uri uri)
    {
        return uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.Unescaped);
    }
}
