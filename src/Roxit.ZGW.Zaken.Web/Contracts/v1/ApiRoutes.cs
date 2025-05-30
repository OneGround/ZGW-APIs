namespace Roxit.ZGW.Zaken.Web.Contracts.v1;

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

        public const string Delete = Base + "/zaken/{id}";

        public const string Search = Base + "/zaken/_zoek";
    }

    public static class ZaakAudittrail
    {
        public const string GetAll = Base + "/zaken/{zaak_uuid}/audittrail";

        public const string Get = Base + "/zaken/{zaak_uuid}/audittrail/{uuid}";
    }

    public class ZaakEigenschappen
    {
        public const string GetAll = Base + "/zaken/{zaak_uuid}/zaakeigenschappen";

        public const string Get = Base + "/zaken/{zaak_uuid}/zaakeigenschappen/{uuid}";

        public const string Create = Base + "/zaken/{zaak_uuid}/zaakeigenschappen";
    }

    public class ZaakBesluiten
    {
        public const string GetAll = Base + "/zaken/{zaak_uuid}/besluiten";

        public const string Get = Base + "/zaken/{zaak_uuid}/besluiten/{uuid}";

        public const string Create = Base + "/zaken/{zaak_uuid}/besluiten";

        public const string Delete = Base + "/zaken/{zaak_uuid}/besluiten/{uuid}";
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
        public const string GetAll = Base + "/resultaten";

        public const string Get = Base + "/resultaten/{id}";

        public const string Create = Base + "/resultaten";

        public const string Update = Base + "/resultaten/{id}";

        public const string Delete = Base + "/resultaten/{id}";
    }

    public static class KlantContacten
    {
        public const string GetAll = Base + "/klantcontacten";

        public const string Get = Base + "/klantcontacten/{id}";

        public const string Create = Base + "/klantcontacten";
    }
}
