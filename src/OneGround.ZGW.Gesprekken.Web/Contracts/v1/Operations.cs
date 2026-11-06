namespace OneGround.ZGW.Gesprekken.Web.Contracts.v1;

public class Operations
{
    public static class Gesprekken
    {
        public const string List = "gesprekken_list";

        public const string Read = "gesprekken_read";

        public const string Create = "gesprekken_create";

        public const string Eindigen = "gesprekken_eindigen";
    }

    public static class Gespreksbijdragen
    {
        public const string Create = "gespreksbijdragen_create";

        public const string Gelezen = "gespreksbijdragen_gelezen";

        public const string Redigeren = "gespreksbijdragen_redigeren";
    }

    public static class Bestanden
    {
        public const string UploadSessie = "bestanden_upload_sessie";

        public const string Read = "bestanden_read";
    }

    public static class Events
    {
        public const string Stream = "events_stream";
    }

    public static class Gespreksdeelnames
    {
        public const string Create = "gespreksdeelnames_create";

        public const string Eindigen = "gespreksdeelnames_eindigen";
    }
}
