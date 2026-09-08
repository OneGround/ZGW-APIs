using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Web.Expands.Fields;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._7;

public static partial class ExpandsServiceCollectionExtensions
{
    /// <summary>
    /// Bouwt het schema waartegen de <c>fields</c> selectie van POST /zaken/_zoek wordt gevalideerd.
    /// De geneste sub-entiteiten volgen de DTO-graaf zoals het expand-mechanisme die ondersteunt.
    /// De toegestane scalaire velden per niveau worden automatisch afgeleid uit de <c>JsonPropertyName</c>
    /// attributen van de betreffende response-DTO's.
    /// </summary>
    public static class EnkelvoudigInformatieObjectFieldsSchema
    {
        public static FieldsSchema Build() =>
            new FieldsSchemaBuilder()
                // EnkelvoudigInformatieObject → sub-entiteiten.
                .Entity<EnkelvoudigInformatieObjectGetResponseDto, InformatieObjectTypeResponseDto>("informatieobjecttype")
                // Diepere niveaus.
                .Entity<InformatieObjectTypeResponseDto, CatalogusResponseDto>("catalogus")
                .Build();
    }
}
