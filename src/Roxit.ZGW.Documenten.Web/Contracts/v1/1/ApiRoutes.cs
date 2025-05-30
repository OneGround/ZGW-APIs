namespace Roxit.ZGW.Documenten.Web.Contracts.v1._1;

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

        public const string Download = Base + "/enkelvoudiginformatieobjecten/{id}/download";

        public const string Lock = Base + "/enkelvoudiginformatieobjecten/{id}/lock";

        public const string Unlock = Base + "/enkelvoudiginformatieobjecten/{id}/unlock";
    }

    public static class BestandsDelen
    {
        public const string Upload = Base + "/bestandsdelen/{id}";
    }
}
