namespace OneGround.ZGW.Besluiten.Web.Contracts.v1;

public class ApiRoutes
{
    private const string Root = "api";

    private const string Version = "v1";

    private const string Base = Root + "/" + Version;

    public static class Besluiten
    {
        public const string GetAll = Base + "/besluiten";

        public const string Get = Base + "/besluiten/{id}";

        public const string Create = Base + "/besluiten";

        public const string Update = Base + "/besluiten/{id}";

        public const string Delete = Base + "/besluiten/{id}";
    }

    public static class BesluitInformatieObjecten
    {
        public const string GetAll = Base + "/besluitinformatieobjecten";

        public const string Get = Base + "/besluitinformatieobjecten/{id}";

        public const string Create = Base + "/besluitinformatieobjecten";

        public const string Delete = Base + "/besluitinformatieobjecten/{id}";
    }

    public static class BesluitAudittrail
    {
        public const string GetAll = Base + "/besluiten/{besluit_uuid}/audittrail";

        public const string Get = Base + "/besluiten/{besluit_uuid}/audittrail/{uuid}";
    }
}
