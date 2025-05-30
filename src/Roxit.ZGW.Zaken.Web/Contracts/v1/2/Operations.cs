namespace Roxit.ZGW.Zaken.Web.Contracts.v1._2;

public class Operations
{
    public static class ZaakObjecten
    {
        public const string List = "zaakobject_list";

        public const string Create = "zaakobject_create";

        public const string Read = "zaakobject_read";

        public const string Delete = "zaakobject_delete";

        public const string Update = "zaakobject_update";

        public const string PartialUpdate = "zaakobject_partial_update";
    }

    public class ZaakEigenschappen
    {
        public const string Update = "zaakeigenschap_update";

        public const string PartialUpdate = "zaakeigenschap_partial_update";

        public const string Delete = "zaakeigenschap_delete";
    }
}
