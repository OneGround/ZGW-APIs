namespace OneGround.ZGW.Zaken.Web.Contracts.v1._7;

public class ApiRoutes
{
    private const string Root = "api";

    private const string Version = "v1";

    private const string Base = Root + "/" + Version;

    public static class ZaaknummerReserveren
    {
        public const string Create = Base + "/zaaknummer_reserveren";
    }
}
