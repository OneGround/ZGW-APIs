using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Contracts.Extensions;
using OneGround.ZGW.Gesprekken.DataModel;

namespace OneGround.ZGW.Gesprekken.Web.Paginering;

/// <summary>
/// Cursor pagination of gesprekken (patterns/pagination, next): a fixed position in the order by aanvangsmoment,
/// so a gesprek that is added while a client pages through the list does not move other gesprekken across a page
/// boundary. The cursor holds the aanvangsmoment of the last gesprek on the page and how many gesprekken with that
/// exact moment were already returned. Opaque to clients.
/// </summary>
public sealed record GesprekCursor(DateTime Aanvang, int Gezien)
{
    public string Encode() => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes($"{Aanvang.Ticks}.{Gezien}"));

    public static bool TryDecode(string value, out GesprekCursor cursor)
    {
        cursor = null;
        try
        {
            var parts = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(value)).Split('.');
            if (parts.Length != 2 || !long.TryParse(parts[0], out var ticks) || !int.TryParse(parts[1], out var gezien) || gezien < 1)
            {
                return false;
            }

            cursor = new GesprekCursor(new DateTime(ticks, DateTimeKind.Utc), gezien);
            return true;
        }
        catch (Exception e) when (e is FormatException or ArgumentException)
        {
            return false;
        }
    }
}

public sealed class CursorPage<T>
{
    public List<T> Results { get; init; } = [];
    public GesprekCursor Next { get; init; }
    public int Count { get; init; }
}

public static class GesprekPaginering
{
    /// <summary>
    /// One page after <paramref name="after"/> (null for the first page), newest first. Ties on aanvangsmoment are
    /// ordered by id.
    /// </summary>
    public static async Task<CursorPage<Gesprek>> PageAsync(
        IQueryable<Gesprek> query,
        GesprekCursor after,
        int size,
        CancellationToken cancellationToken
    )
    {
        var count = await query.CountAsync(cancellationToken);

        if (after != null)
        {
            var moment = after.Aanvang;
            query = query.Where(g => g.AanvangsmomentGesprek <= moment);
        }

        var ordered = query.OrderByDescending(g => g.AanvangsmomentGesprek).ThenBy(g => g.Id);

        // The gesprekken with exactly the moment of the cursor come first; skip the ones already returned
        var rows = await ordered
            .Include(g => g.Bijdragen.OrderByDescending(b => b.Leveringsmoment).Take(1))
                .ThenInclude(b => b.Bijlagen)
            .Skip(after?.Gezien ?? 0)
            .Take(size + 1)
            .ToListAsync(cancellationToken);
        var page = rows.Take(size).ToList();

        GesprekCursor next = null;
        if (rows.Count > size)
        {
            var last = page[^1].AanvangsmomentGesprek;
            var gezien = page.Count(g => g.AanvangsmomentGesprek == last);
            if (after != null && after.Aanvang == last && gezien == page.Count)
            {
                gezien += after.Gezien;
            }

            next = new GesprekCursor(last, gezien);
        }

        return new CursorPage<Gesprek>
        {
            Results = page,
            Next = next,
            Count = count,
        };
    }

    /// <summary>The URL of the next page: this request with the same query parameters and the new cursor.</summary>
    public static string NextLink(HttpRequest request, IQueryParameters queryParameters, GesprekCursor next)
    {
        if (next == null)
        {
            return null;
        }

        var link = $"{request.Scheme}://{request.Host.ToUriComponent()}{request.PathBase}{request.Path}";
        foreach (var parameter in queryParameters.GetParameters().Where(p => p.QueryName != "cursor"))
        {
            var value = parameter.GetValue(queryParameters);
            if (value != null)
            {
                link = QueryHelpers.AddQueryString(link, parameter.QueryName, value);
            }
        }

        return QueryHelpers.AddQueryString(link, "cursor", next.Encode());
    }
}
