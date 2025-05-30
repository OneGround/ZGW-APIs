namespace Roxit.ZGW.Zaken.Web.Contracts.v1;

public class Operations
{
    public static class Zaken
    {
        public const string List = "zaak_list";

        public const string Read = "zaak_read";

        public const string Create = "zaak_create";

        public const string Update = "zaak_update";

        public const string PartialUpdate = "zaak_partial_update";

        public const string Delete = "zaak_delete";

        public const string Search = "zaak__zoek";
    }

    public static class ZaakAudittrail
    {
        public const string List = "audittrail_list";

        public const string Read = "audittrail_read";
    }

    public class ZaakEigenschappen
    {
        public const string List = "zaakeigenschap_list";

        public const string Read = "zaakeigenschap_read";

        public const string Create = "zaakeigenschap_create";
    }

    public class ZaakBesluiten
    {
        public const string List = "zaakbesluit_list";

        public const string Read = "zaakbesluit_read";

        public const string Create = "zaakbesluit_create";

        public const string Delete = "zaakbesluit_delete";
    }

    public static class ZaakStatussen
    {
        public const string List = "status_list";

        public const string Read = "status_read";

        public const string Create = "status_create";
    }

    public static class ZaakObjecten
    {
        public const string List = "zaakobject_list";

        public const string Read = "zaakobject_read";

        public const string Create = "zaakobject_create";
    }

    public static class ZaakRollen
    {
        public const string List = "rol_list";

        public const string Read = "rol_read";

        public const string Create = "rol_create";

        public const string Delete = "rol_delete";
    }

    public static class ZaakInformatieObjecten
    {
        public const string List = "zaakinformatieobject_list";

        public const string Read = "zaakinformatieobject_read";

        public const string Create = "zaakinformatieobject_create";

        public const string Update = "zaakinformatieobject_update";

        public const string PartialUpdate = "zaakinformatieobject_partial_update";

        public const string Delete = "zaakinformatieobject_delete";
    }

    public static class ZaakResultaten
    {
        public const string List = "resultaat_list";

        public const string Read = "resultaat_read";

        public const string Create = "resultaat_create";

        public const string Update = "resultaat_update";

        public const string PartialUpdate = "resultaat_partial_update";

        public const string Delete = "resultaat_delete";
    }

    public static class KlantContacten
    {
        public const string List = "klantcontact_list";

        public const string Read = "klantcontact_read";

        public const string Create = "klantcontact_create";
    }
}
