namespace OneGround.ZGW.Common.Web.Authorization;

public class AuthorizationScopes
{
    public static class Zaken
    {
        private const string _entity = "zaken";

        public const string Read = _entity + ".lezen";

        public const string Update = _entity + ".bijwerken";

        public const string ForcedUpdate = _entity + ".geforceerd-bijwerken";

        public const string Reopen = _entity + ".heropenen";

        public const string Create = _entity + ".aanmaken";

        public const string Delete = _entity + ".verwijderen";

        public static class Statuses
        {
            public const string Add = _entity + ".statussen.toevoegen";
        }
    }

    public static class Catalogi
    {
        private const string _entity = "catalogi";

        public const string Read = _entity + ".lezen";

        public const string Write = _entity + ".schrijven";

        public const string ForcedDelete = _entity + ".geforceerd-verwijderen";

        public const string ForcedUpdate = _entity + ".geforceerd-bijwerken";
    }

    public static class Besluiten
    {
        private const string _entity = "besluiten";

        public const string Read = _entity + ".lezen";

        public const string Create = _entity + ".aanmaken";

        public const string Update = _entity + ".bijwerken";

        public const string Delete = _entity + ".verwijderen";
    }

    public static class Notificaties
    {
        private const string _entity = "notificaties";

        public const string Consume = _entity + ".consumeren";

        public const string Produce = _entity + ".publiceren";
    }

    public static class Documenten
    {
        private const string _entity = "documenten";

        public const string Read = _entity + ".lezen";

        public const string Create = _entity + ".aanmaken";

        public const string Update = _entity + ".bijwerken";

        public const string Delete = _entity + ".verwijderen";

        public const string Lock = _entity + ".lock";

        public const string ForcedUnlock = _entity + ".geforceerd-unlock";
    }

    public static class Autorisaties
    {
        private const string _entity = "autorisaties";

        public const string Read = _entity + ".lezen";

        public const string Update = _entity + ".bijwerken";
    }

    public static class AuditTrails
    {
        private const string _entity = "audittrails";

        public const string Read = _entity + ".lezen";
    }
}
