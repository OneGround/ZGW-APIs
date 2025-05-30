namespace Roxit.ZGW.Besluiten.Web.Contracts.v1;

public class Operations
{
    public static class Besluiten
    {
        public const string List = "besluit_list";

        public const string Read = "besluit_read";

        public const string Create = "besluit_create";

        public const string Update = "besluit_update";

        public const string PartialUpdate = "besluit_partial_update";

        public const string Delete = "besluit_delete";
    }

    public static class BesluitInformatieObjecten
    {
        public const string List = "besluitinformatieobject_list";

        public const string Read = "besluitinformatieobject_read";

        public const string Create = "besluitinformatieobject_create";

        public const string Delete = "besluitinformatieobject_delete";
    }

    public static class BesluitAudittrail
    {
        public const string List = "audittrail_list";

        public const string Read = "audittrail_read";
    }
}
