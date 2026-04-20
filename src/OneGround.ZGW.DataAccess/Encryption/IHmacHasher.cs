namespace OneGround.ZGW.DataAccess.Encryption;

public interface IHmacHasher
{
    string ComputeHash(string plaintext);
}
