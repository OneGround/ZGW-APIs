namespace Roxit.ZGW.Catalogi.Web.Contracts.v1;

public class ApiRoutes
{
    private const string Root = "api";

    private const string Version = "v1";

    private const string Base = Root + "/" + Version;

    public static class ZaakTypen
    {
        public const string GetAll = Base + "/zaaktypen";

        public const string Get = Base + "/zaaktypen/{id}";

        public const string Create = Base + "/zaaktypen";

        public const string Update = Base + "/zaaktypen/{id}";

        public const string Delete = Base + "/zaaktypen/{id}";

        public const string Publish = Base + "/zaaktypen/{id}/publish";
    }

    public static class StatusTypen
    {
        public const string GetAll = Base + "/statustypen";

        public const string Get = Base + "/statustypen/{id}";

        public const string Create = Base + "/statustypen";

        public const string Update = Base + "/statustypen/{id}";

        public const string Delete = Base + "/statustypen/{id}";
    }

    public static class RolTypen
    {
        public const string Get = Base + "/roltypen/{id}";

        public const string Create = Base + "/roltypen";

        public const string GetAll = Base + "/roltypen";

        public const string Update = Base + "/roltypen/{id}";

        public const string Delete = Base + "/roltypen/{id}";
    }

    public static class ZaakTypeInformatieObjectTypen
    {
        public const string GetAll = Base + "/zaaktype-informatieobjecttypen";
        public const string Get = Base + "/zaaktype-informatieobjecttypen/{id}";
        public const string Create = Base + "/zaaktype-informatieobjecttypen";
        public const string Update = Base + "/zaaktype-informatieobjecttypen/{id}";
        public const string Delete = Base + "/zaaktype-informatieobjecttypen/{id}";
    }

    public static class ResultaatTypen
    {
        public const string GetAll = Base + "/resultaattypen";

        public const string Get = Base + "/resultaattypen/{id}";

        public const string Create = Base + "/resultaattypen";

        public const string Update = Base + "/resultaattypen/{id}";

        public const string Delete = Base + "/resultaattypen/{id}";
    }

    public static class Catalogussen
    {
        private const string CatalogussenBase = "/catalogussen";

        public const string GetAll = Base + CatalogussenBase;
        public const string Get = Base + CatalogussenBase + "/{id}";
        public const string Create = Base + CatalogussenBase;
    }

    public static class InformatieObjectTypen
    {
        private const string InformatieObjectTypenBase = "/informatieobjecttypen";

        public const string GetAll = Base + InformatieObjectTypenBase;
        public const string Get = Base + InformatieObjectTypenBase + "/{id}";
        public const string Create = Base + InformatieObjectTypenBase;
        public const string Update = Base + InformatieObjectTypenBase + "/{id}";
        public const string Delete = Base + InformatieObjectTypenBase + "/{id}";
        public const string Publish = Base + InformatieObjectTypenBase + "/{id}" + "/publish";
    }

    public static class Eigenschappen
    {
        public const string GetAll = Base + "/eigenschappen";

        public const string Get = Base + "/eigenschappen/{id}";

        public const string Create = Base + "/eigenschappen";

        public const string Update = Base + "/eigenschappen/{id}";

        public const string Delete = Base + "/eigenschappen/{id}";
    }

    public static class BesluitTypen
    {
        public const string Get = Base + "/besluittypen/{id}";

        public const string Create = Base + "/besluittypen";

        public const string GetAll = Base + "/besluittypen";

        public const string Update = Base + "/besluittypen/{id}";

        public const string Delete = Base + "/besluittypen/{id}";

        public const string Publish = Base + "/besluittypen/{id}/publish";
    }
}
