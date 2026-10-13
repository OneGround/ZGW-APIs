namespace OneGround.ZGW.Documenten.ServiceAgent.v1._7;

// Note: User-authenticated (AuthorizationType.UserAccount, forwards the real caller's JWT) variant of
// IDocumentenServiceAgent, for cross-API expands that must be evaluated by DRC's own authorization
// model as the actual calling client, not as this backend's own per-RSIN service credential (the
// default for IDocumentenServiceAgent/AddDocumentenServiceAgent_v1_7). Mirrors the (obsolete) v1._5
// IUserAuthDocumentenServiceAgent, but on the current (v1._7) DTO/contract shape, so ZRC->DRC 1.7
// expands pick up DRC 1.7 and ZTC 1.3.3's newer fields. The v1._5 variant is deliberately left
// untouched -- it exists specifically for v1._5 expands (see AddUserAuthDocumentenServiceAgent_v1_5's
// remarks in Startup.cs).
public interface IUserAuthDocumentenServiceAgent : IDocumentenServiceAgent;
