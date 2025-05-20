namespace OneGround.ZGW.Common.Authentication
{
    public interface IOrganisationContextFactory
    {
        OrganisationContext Create(string rsin);
    }

    public class OrganisationContextFactory : IOrganisationContextFactory
    {
        private readonly IOrganisationContextAccessor _organisationContextAccessor;

        public OrganisationContextFactory(IOrganisationContextAccessor organisationContextAccessor)
        {
            _organisationContextAccessor = organisationContextAccessor;
        }

        public OrganisationContext Create(string rsin)
        {
            var context = new OrganisationContext(rsin);

            _organisationContextAccessor.OrganisationContext = context;

            return context;
        }
    }
}
