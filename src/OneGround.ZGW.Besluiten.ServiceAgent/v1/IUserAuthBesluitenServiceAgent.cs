namespace OneGround.ZGW.Besluiten.ServiceAgent.v1;

// Note: Variant of IBesluitenServiceAgent that forwards the real caller's JWT (AuthorizationType.UserAccount), so BRC authorizes the actual client.
// Register it with AddUserAuthBesluitenServiceAgent: IBesluitenServiceAgent uses the per-RSIN service credential (used by the "besluit" expand).
public interface IUserAuthBesluitenServiceAgent : IBesluitenServiceAgent;
