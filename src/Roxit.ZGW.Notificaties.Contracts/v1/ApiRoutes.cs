namespace Roxit.ZGW.Notificaties.Contracts.v1;

public class ApiRoutes
{
    private const string Root = "api";

    private const string Version = "v1";

    private const string Base = Root + "/" + Version;

    public static class Kanaal
    {
        public const string GetAll = Base + "/kanaal";

        public const string Get = Base + "/kanaal/{id}";

        public const string Create = Base + "/kanaal";
    }

    public static class Abonnement
    {
        public const string GetAll = Base + "/abonnement";

        public const string Get = Base + "/abonnement/{id}";

        public const string Create = Base + "/abonnement";

        public const string Update = Base + "/abonnement/{id}";

        public const string Delete = Base + "/abonnement/{id}";
    }

    public static class Notificaties
    {
        public const string Create = Base + "/notificaties";
    }
}
