namespace OneGround.ZGW.Notificaties.Contracts.v1;

public class Operations
{
    public static class Kanaal
    {
        public const string List = "kanaal_list";

        public const string Read = "kanaal_read";

        public const string Create = "kanaal_create";
    }

    public static class Abonnement
    {
        public const string List = "abonnement_list";

        public const string Read = "abonnement_read";

        public const string Create = "abonnement_create";

        public const string Update = "abonnement_update";

        public const string PartialUpdate = "abonnement_partial_update";

        public const string Delete = "abonnement_delete";
    }

    public static class Notificaties
    {
        public const string Create = "notificaties_create";
    }
}
