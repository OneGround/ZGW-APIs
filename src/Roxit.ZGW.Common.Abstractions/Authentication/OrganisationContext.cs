namespace Roxit.ZGW.Common.Authentication
{
    public class OrganisationContext
    {
        public string Rsin { get; }

        public OrganisationContext(string rsin)
        {
            Rsin = rsin;
        }
    }
}
