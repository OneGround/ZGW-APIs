namespace OneGround.ZGW.Common.DataModel.Encryption;

public interface IHmacHasher
{
    string ComputeHash(string plaintext);
}
