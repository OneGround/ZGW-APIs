namespace OneGround.ZGW.Referentielijsten.Web.Contracts.v1;

public class ApiRoutes
{
    private const string Root = "api";

    private const string Version = "v1";

    private const string Base = Root + "/" + Version;

    public static class CommunicatieKanalen
    {
        public const string GetAll = Base + "/communicatiekanalen";

        public const string Get = Base + "/communicatiekanalen/{id}";
    }

    public static class ResultaatTypeomschrijvingen
    {
        public const string GetAll = Base + "/resultaattypeomschrijvingen";

        public const string Get = Base + "/resultaattypeomschrijvingen/{id}";
    }

    public static class Resultaten
    {
        public const string GetAll = Base + "/resultaten";

        public const string Get = Base + "/resultaten/{id}";
    }

    public static class ProcesTypen
    {
        public const string GetAll = Base + "/procestypen";

        public const string Get = Base + "/procestypen/{id}";
    }
}
