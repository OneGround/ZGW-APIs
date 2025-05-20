using System.Threading;

namespace OneGround.ZGW.Common.Authentication
{
    public interface IOrganisationContextAccessor
    {
        OrganisationContext OrganisationContext { get; set; }
    }

    public class OrganisationContextAccessor : IOrganisationContextAccessor
    {
        private static readonly AsyncLocal<OrganisationContext> _organisationContext = new AsyncLocal<OrganisationContext>();

        public OrganisationContext OrganisationContext
        {
            get => _organisationContext.Value;
            set => _organisationContext.Value = value;
        }
    }
}
