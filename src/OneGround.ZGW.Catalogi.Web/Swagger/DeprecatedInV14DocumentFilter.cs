using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi;
using OneGround.ZGW.Catalogi.Web.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OneGround.ZGW.Catalogi.Web.Swagger;

/// <summary>
/// Marks the properties and query parameters that the VNG Catalogi API 1.4.0 specification flags as deprecated as `deprecated` in the OpenAPI
/// document of version 1.4 only (the documents of the older versions are not touched). It is a documentation change only: the properties and
/// the filters keep working. Only the members that exist in our contracts are listed.
/// </summary>
public class DeprecatedInV14DocumentFilter : IDocumentFilter
{
    // The name of the OpenAPI document of version 1.4: the group name is the version itself (ZgwApiVersion ignores the group name format)
    internal const string DocumentNameV14 = Api.LatestVersion_1_4;

    private const string Responses = "OneGround.ZGW.Catalogi.Contracts.v1._4.Responses.";

    // The v1.4 endpoints use the v1.3 request DTOs
    private const string Requests = "OneGround.ZGW.Catalogi.Contracts.v1._3.Requests.";

    private static readonly string[] Geldigheid = ["beginGeldigheid", "eindeGeldigheid"];
    private static readonly string[] Object = ["beginObject", "eindeObject"];

    internal static readonly IReadOnlyDictionary<string, string[]> DeprecatedProperties = new Dictionary<string, string[]>
    {
        // Responses
        [Responses + "BesluitTypeResponseDto"] = [.. Object],
        [Responses + "EigenschapResponseDto"] = [.. Geldigheid, .. Object],
        [Responses + "InformatieObjectTypeResponseDto"] = [.. Object],
        [Responses + "ResultaatTypeResponseDto"] = ["catalogus", .. Geldigheid, .. Object, "indicatieSpecifiek"],
        [Responses + "RolTypeResponseDto"] = ["catalogus", .. Geldigheid, .. Object],
        [Responses + "StatusTypeResponseDto"] = [.. Geldigheid, .. Object],
        [Responses + "ZaakObjectTypeResponseDto"] = [.. Geldigheid, .. Object, "resultaattypen", "statustypen", "resultaattypeOmschrijving"],
        [Responses + "ZaakTypeResponseDto"] = [.. Object, "versiedatum"],
        // Requests
        [Requests + "BesluitTypeRequestDto"] = [.. Object],
        [Requests + "EigenschapRequestDto"] = [.. Geldigheid, .. Object],
        [Requests + "InformatieObjectTypeRequestDto"] = [.. Object],
        [Requests + "ResultaatTypeRequestDto"] = ["catalogus", .. Geldigheid, .. Object, "indicatieSpecifiek"],
        [Requests + "RolTypeRequestDto"] = ["catalogus", .. Geldigheid, .. Object],
        [Requests + "StatusTypeRequestDto"] = [.. Geldigheid, .. Object],
        [Requests + "ZaakObjectTypeRequestDto"] = [.. Geldigheid, .. Object],
        [Requests + "ZaakTypeRequestDto"] = [.. Object, "versiedatum"],
    };

    // Path (suffix) -> query parameters
    internal static readonly IReadOnlyDictionary<string, string[]> DeprecatedQueryParameters = new Dictionary<string, string[]>
    {
        ["/zaakobjecttypen"] = ["datumBeginGeldigheid", "datumEindeGeldigheid"],
    };

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (!string.Equals(context.DocumentName, DocumentNameV14, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var (schemaId, propertyNames) in DeprecatedProperties)
        {
            if (swaggerDoc.Components?.Schemas == null || !swaggerDoc.Components.Schemas.TryGetValue(schemaId, out var schema))
            {
                continue;
            }

            foreach (var propertyName in propertyNames)
            {
                // Note: a property that is a reference to another schema cannot be flagged (not the case for the properties above)
                if (schema.Properties != null && schema.Properties.TryGetValue(propertyName, out var property) && property is OpenApiSchema inline)
                {
                    inline.Deprecated = true;
                }
            }
        }

        foreach (var (pathSuffix, parameterNames) in DeprecatedQueryParameters)
        {
            foreach (var pathItem in swaggerDoc.Paths.Where(p => p.Key.EndsWith(pathSuffix, StringComparison.OrdinalIgnoreCase)).Select(p => p.Value))
            {
                foreach (var operation in pathItem.Operations?.Values.AsEnumerable() ?? [])
                {
                    foreach (var parameter in operation.Parameters ?? [])
                    {
                        if (
                            parameter.In == ParameterLocation.Query
                            && parameterNames.Contains(parameter.Name)
                            && parameter is OpenApiParameter inline
                        )
                        {
                            inline.Deprecated = true;
                        }
                    }
                }
            }
        }
    }
}
