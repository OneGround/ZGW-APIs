namespace Roxit.ZGW.Zaken.Web.Contracts.v1._5;

public class ApiRoutes
{
    private const string Root = "api";

    private const string Version = "v1";

    private const string Base = Root + "/" + Version;

    public static class Zaken
    {
        public const string GetAll = Base + "/zaken";

        public const string Get = Base + "/zaken/{id}";

        public const string Create = Base + "/zaken";

        public const string Update = Base + "/zaken/{id}";

        public const string Search = Base + "/zaken/_zoek";
    }

    public class ZaakEigenschappen
    {
        public const string Get = Base + "/zaken/{zaak_uuid}/zaakeigenschappen/{uuid}";
    }

    public static class ZaakStatussen
    {
        public const string GetAll = Base + "/statussen";

        public const string Get = Base + "/statussen/{id}";

        public const string Create = Base + "/statussen";
    }

    public static class ZaakObjecten
    {
        public const string GetAll = Base + "/zaakobjecten";

        public const string Get = Base + "/zaakobjecten/{id}";

        public const string Create = Base + "/zaakobjecten";

        public const string Delete = Base + "/zaakobjecten/{id}";

        public const string Update = Base + "/zaakobjecten/{id}";
    }

    public static class ZaakRollen
    {
        public const string GetAll = Base + "/rollen";

        public const string Get = Base + "/rollen/{id}";

        public const string Create = Base + "/rollen";

        public const string Delete = Base + "/rollen/{id}";
    }

    public static class ZaakInformatieObjecten
    {
        public const string GetAll = Base + "/zaakinformatieobjecten";

        public const string Get = Base + "/zaakinformatieobjecten/{id}";

        public const string Create = Base + "/zaakinformatieobjecten";

        public const string Update = Base + "/zaakinformatieobjecten/{id}";

        public const string Delete = Base + "/zaakinformatieobjecten/{id}";
    }

    public static class ZaakResultaten
    {
        public const string Get = Base + "/resultaten/{id}";
    }

    public class ZaakVerzoeken
    {
        public const string GetAll = Base + "/zaakverzoeken";

        public const string Get = Base + "/zaakverzoeken/{id}";

        public const string Create = Base + "/zaakverzoeken";

        public const string Delete = Base + "/zaakverzoeken/{id}";
    }

    public class ZaakContactmomenten
    {
        public const string GetAll = Base + "/zaakcontactmomenten";

        public const string Get = Base + "/zaakcontactmomenten/{id}";

        public const string Create = Base + "/zaakcontactmomenten";

        public const string Delete = Base + "/zaakcontactmomenten/{id}";
    }
}
