using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.OpenApi;
using OneGround.ZGW.Catalogi.Web.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.SwaggerTests;

public class DeprecatedInV14DocumentFilterTests
{
    private const string ZaakObjectTypeResponse = "OneGround.ZGW.Catalogi.Contracts.v1._4.Responses.ZaakObjectTypeResponseDto";
    private const string ZaakObjectTypeRequest = "OneGround.ZGW.Catalogi.Contracts.v1._3.Requests.ZaakObjectTypeRequestDto";

    private static OpenApiDocument CreateDocument()
    {
        OpenApiSchema SchemaWith(params string[] names) =>
            new() { Properties = names.ToDictionary(n => n, n => (IOpenApiSchema)new OpenApiSchema { Type = JsonSchemaType.String }) };

        var operation = new OpenApiOperation
        {
            Parameters =
            [
                new OpenApiParameter { Name = "datumBeginGeldigheid", In = ParameterLocation.Query },
                new OpenApiParameter { Name = "datumEindeGeldigheid", In = ParameterLocation.Query },
                new OpenApiParameter { Name = "datumGeldigheid", In = ParameterLocation.Query },
            ],
        };

        return new OpenApiDocument
        {
            Components = new OpenApiComponents
            {
                Schemas = new Dictionary<string, IOpenApiSchema>
                {
                    [ZaakObjectTypeResponse] = SchemaWith(
                        "beginGeldigheid",
                        "beginObject",
                        "resultaattypen",
                        "statustypen",
                        "resultaattypeOmschrijving",
                        "url"
                    ),
                    [ZaakObjectTypeRequest] = SchemaWith("beginGeldigheid", "eindeObject", "url"),
                },
            },
            Paths = new OpenApiPaths
            {
                ["/zaakobjecttypen"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation> { [HttpMethod.Get] = operation },
                },
            },
        };
    }

    private static void Apply(OpenApiDocument document, string documentName) =>
        new DeprecatedInV14DocumentFilter().Apply(document, new DocumentFilterContext([], null, new SchemaRepository(documentName)));

    private static bool IsDeprecated(OpenApiDocument document, string schemaId, string property) =>
        ((OpenApiSchema)document.Components.Schemas[schemaId].Properties[property]).Deprecated;

    [Fact]
    public void Apply_DocumentOfVersion14_MarksTheDeprecatedPropertiesOfResponsesAndRequests()
    {
        var document = CreateDocument();

        Apply(document, "1.4.0");

        foreach (var property in new[] { "beginGeldigheid", "beginObject", "resultaattypen", "statustypen", "resultaattypeOmschrijving" })
        {
            Assert.True(IsDeprecated(document, ZaakObjectTypeResponse, property), property);
        }
        Assert.False(IsDeprecated(document, ZaakObjectTypeResponse, "url"));
        Assert.True(IsDeprecated(document, ZaakObjectTypeRequest, "beginGeldigheid"));
        Assert.True(IsDeprecated(document, ZaakObjectTypeRequest, "eindeObject"));
        Assert.False(IsDeprecated(document, ZaakObjectTypeRequest, "url"));
    }

    [Fact]
    public void Apply_DocumentOfVersion14_MarksOnlyTheDeprecatedQueryParameters()
    {
        var document = CreateDocument();

        Apply(document, "1.4.0");

        var parameters = document.Paths["/zaakobjecttypen"].Operations[HttpMethod.Get].Parameters.Cast<OpenApiParameter>().ToDictionary(p => p.Name);
        Assert.True(parameters["datumBeginGeldigheid"].Deprecated);
        Assert.True(parameters["datumEindeGeldigheid"].Deprecated);
        Assert.False(parameters["datumGeldigheid"].Deprecated);
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("1.2.0")]
    [InlineData("1.3.1")]
    public void Apply_DocumentOfAnOlderVersion_ChangesNothing(string documentName)
    {
        var document = CreateDocument();

        Apply(document, documentName);

        Assert.False(IsDeprecated(document, ZaakObjectTypeResponse, "beginGeldigheid"));
        Assert.False(IsDeprecated(document, ZaakObjectTypeRequest, "beginGeldigheid"));
        Assert.All(
            document.Paths["/zaakobjecttypen"].Operations[HttpMethod.Get].Parameters.Cast<OpenApiParameter>(),
            p => Assert.False(p.Deprecated)
        );
    }
}
