namespace OneGround.ZGW.Zaken.Web.Contracts.v1._5;

public class Operations
{
    public static class Zaken
    {
        public const string List = "zaak_list";

        public const string Read = "zaak_read";

        public const string ReadHead = "zaak_read_head";

        public const string Create = "zaak_create";

        public const string Update = "zaak_update";

        public const string PartialUpdate = "zaak_partial_update";

        public const string Search = "zaak__zoek";
    }

    public class ZaakEigenschappen
    {
        public const string Read = "zaakeigenschap_read";

        public const string ReadHead = "zaakeigenschap_read_head";
    }

    public static class ZaakStatussen
    {
        public const string List = "status_list";

        public const string Read = "status_read";

        public const string ReadHead = "status_read_head";

        public const string Create = "status_create";
    }

    public static class ZaakObjecten
    {
        public const string List = "zaakobject_list";

        public const string Create = "zaakobject_create";

        public const string Read = "zaakobject_read";

        public const string ReadHead = "zaakobject_read_head";

        public const string Delete = "zaakobject_delete";

        public const string Update = "zaakobject_update";

        public const string PartialUpdate = "zaakobject_partial_update";
    }

    public static class ZaakRollen
    {
        public const string List = "rol_list";

        public const string Read = "rol_read";

        public const string ReadHead = "rol_read_head";

        public const string Create = "rol_create";

        public const string Delete = "rol_delete";
    }

    public static class ZaakInformatieObjecten
    {
        public const string List = "zaakinformatieobject_list";

        public const string Read = "zaakinformatieobject_read";

        public const string ReadHead = "zaakinformatieobject_read_head";

        public const string Create = "zaakinformatieobject_create";

        public const string Update = "zaakinformatieobject_update";

        public const string PartialUpdate = "zaakinformatieobject_partial_update";

        public const string Delete = "zaakinformatieobject_delete";
    }

    public static class ZaakResultaten
    {
        public const string Read = "resultaat_read";

        public const string ReadHead = "resultaat_read_head";
    }

    public static class ZaakVerzoeken
    {
        public const string List = "zaakverzoek_list";

        public const string Read = "zaakverzoek_read";

        public const string Create = "zaakverzoek_create";

        public const string Delete = "zaakverzoek_delete";
    }

    public static class ZaakContactmomenten
    {
        public const string List = "zaakcontactmomenten_list";

        public const string Read = "zaakcontactmomenten_read";

        public const string Create = "zaakcontactmomenten_create";

        public const string Delete = "zaakcontactmomenten_delete";
    }
}
