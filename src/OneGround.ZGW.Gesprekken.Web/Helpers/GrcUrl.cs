using System;
using FluentValidation;
using Mapster;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Gesprekken.Web.Helpers;

/// <summary>
/// References between resources of this API are URLs, as in the other ZGW APIs. Internally they are kept as ids.
/// </summary>
public static class GrcUrl
{
    public const string Gesprekken = "gesprekken";
    public const string Gespreksdeelnames = "gespreksdeelnames";
    public const string Gespreksbijdragen = "gespreksbijdragen";
    public const string Bestanden = "bestanden";

    /// <summary>
    /// The id in a URL of the given collection, e.g. ".../gesprekken/{uuid}". Only the last two path segments are
    /// looked at, so the URL works behind any host or path prefix.
    /// </summary>
    public static bool TryGetId(string url, string collection, out Guid id)
    {
        id = Guid.Empty;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var segments = uri.AbsolutePath.TrimEnd('/').Split('/');
        return segments.Length >= 2
            && string.Equals(segments[^2], collection, StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(segments[^1], out id);
    }

    public static Guid? GetIdOrNull(string url, string collection) => TryGetId(url, collection, out var id) ? id : null;

    /// <summary>The URL of a resource of this API, for use inside a Mapster mapping.</summary>
    public static string Of(string collection, Guid id) => MapContext.Current.GetService<IEntityUriService>().GetUri(collection, id.ToString());

    public static string Of(string collection, Guid? id) => id.HasValue ? Of(collection, id.Value) : null;

    public static IRuleBuilderOptions<T, string> IsGrcUrl<T>(this IRuleBuilder<T, string> ruleBuilder, string collection) =>
        ruleBuilder
            .Must(value => string.IsNullOrEmpty(value) || TryGetId(value, collection, out _))
            .WithMessage($"Geef de URL van een resource in /{collection}.")
            .WithErrorCode(ErrorCode.InvalidResource);
}
