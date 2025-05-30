namespace Roxit.ZGW.Autorisaties.Web.Contracts;

public class ApiRoutes
{
    private const string Root = "api";

    private const string Version = "v1";

    private const string Base = Root + "/" + Version;

    public static class Applicaties
    {
        private const string ApplicatiesBase = "/applicaties";

        public const string GetAll = Base + ApplicatiesBase;
        public const string Get = Base + ApplicatiesBase + "/{id}";
        public const string GetByConsumer = Base + ApplicatiesBase + "/consumer";
        public const string Create = Base + ApplicatiesBase;
        public const string Update = Base + ApplicatiesBase + "/{id}";
        public const string Delete = Base + ApplicatiesBase + "/{id}";
    }
}
