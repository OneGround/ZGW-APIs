namespace OneGround.ZGW.DataAccess;

public interface IDbUserContext
{
    /// <summary>
    /// In the ZGW.API represents client identifier
    /// </summary>
    string UserId { get; }
}
