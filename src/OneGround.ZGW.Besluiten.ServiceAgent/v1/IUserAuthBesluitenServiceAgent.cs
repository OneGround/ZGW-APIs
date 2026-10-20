namespace OneGround.ZGW.Besluiten.ServiceAgent.v1;

// Note: User-authenticated (AuthorizationType.UserAccount, forwards the real caller's JWT) variant of IBesluitenServiceAgent, for cross-API
// expands that must be evaluated by BRC's own authorization model as the actual calling client, not as this backend's own per-RSIN service
// credential (the default for IBesluitenServiceAgent/AddBesluitenServiceAgent). Used by the ZRC 1.7 expand "besluit" on a ZAAKBESLUIT.
public interface IUserAuthBesluitenServiceAgent : IBesluitenServiceAgent;
