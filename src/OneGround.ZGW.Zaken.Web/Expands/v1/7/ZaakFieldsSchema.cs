using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Web.Expands.Fields;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Bouwt het schema waartegen de <c>fields</c> selectie van POST /zaken/_zoek wordt gevalideerd.
/// De geneste sub-entiteiten volgen de DTO-graaf zoals het expand-mechanisme die ondersteunt.
/// </summary>
public static class ZaakFieldsSchema
{
    public static FieldsSchema Build() =>
        new FieldsSchemaBuilder()
            .Entity<ZaakResponseDto, ZaakTypeResponseDto>("zaaktype")
            .Entity<ZaakTypeResponseDto, CatalogusResponseDto>("catalogus")
            .Build();
}
