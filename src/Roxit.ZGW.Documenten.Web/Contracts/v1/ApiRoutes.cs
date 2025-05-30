namespace Roxit.ZGW.Documenten.Web.Contracts.v1;

public class ApiRoutes
{
    private const string Root = "api";

    private const string Version = "v1";

    private const string Base = Root + "/" + Version;

    public static class EnkelvoudigInformatieObjecten
    {
        public const string GetAll = Base + "/enkelvoudiginformatieobjecten";

        public const string Get = Base + "/enkelvoudiginformatieobjecten/{id}";

        public const string Create = Base + "/enkelvoudiginformatieobjecten";

        public const string Update = Base + "/enkelvoudiginformatieobjecten/{id}";

        public const string Delete = Base + "/enkelvoudiginformatieobjecten/{id}";

        public const string Download = Base + "/enkelvoudiginformatieobjecten/{id}/download";

        public const string Lock = Base + "/enkelvoudiginformatieobjecten/{id}/lock";

        public const string Unlock = Base + "/enkelvoudiginformatieobjecten/{id}/unlock";
    }

    public static class EnkelvoudigInformatieObjectAudittrail
    {
        public const string GetAll = Base + "/enkelvoudiginformatieobjecten/{enkelvoudiginformatieobject_uuid}/audittrail";

        public const string Get = Base + "/enkelvoudiginformatieobjecten/{enkelvoudiginformatieobject_uuid}/audittrail/{uuid}";
    }

    public static class ObjectInformatieObjecten
    {
        public const string GetAll = Base + "/objectinformatieobjecten";

        public const string Get = Base + "/objectinformatieobjecten/{id}";

        public const string Create = Base + "/objectinformatieobjecten";

        public const string Delete = Base + "/objectinformatieobjecten/{id}";
    }

    public static class GebruiksRechten
    {
        public const string GetAll = Base + "/gebruiksrechten";

        public const string Get = Base + "/gebruiksrechten/{id}";

        public const string Create = Base + "/gebruiksrechten";

        public const string Update = Base + "/gebruiksrechten/{id}";

        public const string Delete = Base + "/gebruiksrechten/{id}";
    }
}
