namespace OneGround.ZGW.Gesprekken.Web.Contracts.v1;

public class ApiRoutes
{
    private const string Root = "api";

    private const string Version = "v1";

    private const string Base = Root + "/" + Version;

    public static class Gesprekken
    {
        public const string GetAll = Base + "/gesprekken";

        public const string Get = Base + "/gesprekken/{id}";

        public const string Create = Base + "/gesprekken";

        public const string Eindigen = Base + "/gesprekken/{id}/eindigen";
    }

    public static class Gespreksbijdragen
    {
        public const string Create = Base + "/gespreksbijdragen";

        public const string Gelezen = Base + "/gespreksbijdragen/{id}/gelezen";

        public const string Redigeren = Base + "/gespreksbijdragen/{id}/redigeren";
    }

    public static class Bestanden
    {
        public const string UploadSessie = Base + "/bestanden/upload-sessie";

        public const string Get = Base + "/bestanden/{fileId}";
    }

    public static class Events
    {
        public const string Stream = Base + "/events";
    }

    public static class Gespreksdeelnames
    {
        public const string Create = Base + "/gespreksdeelnames";

        public const string Eindigen = Base + "/gespreksdeelnames/{id}/eindigen";
    }
}
