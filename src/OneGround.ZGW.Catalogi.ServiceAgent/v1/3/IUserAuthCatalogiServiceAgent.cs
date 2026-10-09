namespace OneGround.ZGW.Catalogi.ServiceAgent.v1._3;

// Note: User-authenticated (AuthorizationType.UserAccount, forwards the real caller's JWT) variant of ICatalogiServiceAgent, for cross-API
// expands that must be evaluated by ZTC's own authorization model as the actual calling client, not as this backend's own per-RSIN
// service credential (the default for ICatalogiServiceAgent). Deliberately registered WITHOUT the distributed response cache: that cache is
// keyed by RSIN and url, not by user, so a response fetched by a caller with ZTC access would be served to a caller without it.
// The expands do use that cache after all, but only once ZTC has accepted the caller in the same request: see CatalogiServiceAgentDecorator.
public interface IUserAuthCatalogiServiceAgent : ICatalogiServiceAgent;
