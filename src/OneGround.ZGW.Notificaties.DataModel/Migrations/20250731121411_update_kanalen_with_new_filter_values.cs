using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneGround.ZGW.Notificaties.DataModel.Migrations
{
    /// <inheritdoc />
    public partial class update_kanalen_with_new_filter_values : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
update kanalen
set filters = '{bronorganisatie,zaaktype,vertrouwelijkheidaanduiding,archiefstatus,archiefnominatie,opdrachtgevende_organisatie,catalogus,domein,zaaktype_identificatie,is_eindzaakstatus}'
where naam = 'zaken';"
            );

            migrationBuilder.Sql(
                @"
update kanalen
set filters = '{verantwoordelijke_organisatie,besluittype,besluittype_omschrijving,catalogus,domein}'
where naam = 'besluiten';"
            );

            migrationBuilder.Sql(
                @"
update kanalen
set filters = '{bronorganisatie,informatieobjecttype,vertrouwelijkheidaanduiding,informatieobjecttype_omschrijving,catalogus,domein,status,inhoud_is_vervallen}'
where naam = 'documenten';"
            );

            migrationBuilder.Sql(
                @"
update kanalen
set filters = '{catalogus,domein}'
where naam = 'zaaktypen';"
            );

            migrationBuilder.Sql(
                @"
update kanalen
set filters = '{catalogus,domein}'
where naam = 'besluittypen';"
            );

            migrationBuilder.Sql(
                @"
update kanalen
set filters = '{catalogus,domein}'
where naam = 'informatieobjecttypen';"
            );
        }
    }
}
