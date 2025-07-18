namespace KeycloakSetup.Configuration
{
    public class KeycloakSettings
    {
        public required string BaseUrl { get; set; }
        public required string AdminUsername { get; set; }
        public required string AdminPassword { get; set; }
        public required string RealmName { get; set; }
        public required List<ClientConfiguration> Clients { get; set; }
    }

    public class ClientConfiguration
    {
        public required string ClientId { get; set; }
        public required string ClientName { get; set; }
        public required string ClientDescription { get; set; }
        public required string Rsin { get; set; }
    }
} 