namespace OneGround.ZGW.Zaken.Web.Contracts.v1._6;

public class ApiRoutes
{
    private const string Root = "api";

    private const string Version = "v1";

    private const string Base = Root + "/" + Version;

    public static class Zaken
    {
        public const string GetUitgebreid = Base + "/zaken/zaak_uitgebreid"; // TODO: make it: "/zaken/_get" ?

        public const string Search = Base + "/zaken/_zoek";
    }
}
