namespace Roxit.ZGW.Zaken.Web.Contracts.v1._2;

public class ApiRoutes
{
    private const string Root = "api";

    private const string Version = "v1";

    private const string Base = Root + "/" + Version;

    public static class ZaakObjecten
    {
        public const string GetAll = Base + "/zaakobjecten";

        public const string Get = Base + "/zaakobjecten/{id}";

        public const string Create = Base + "/zaakobjecten";

        public const string Delete = Base + "/zaakobjecten/{id}";

        public const string Update = Base + "/zaakobjecten/{id}";
    }

    public class ZaakEigenschappen
    {
        // Note: Added in v1.2 with existing operations for v1.0
        public const string Update = Base + "/zaken/{zaak_uuid}/zaakeigenschappen/{uuid}";

        public const string Delete = Base + "/zaken/{zaak_uuid}/zaakeigenschappen/{uuid}";
    }
}
