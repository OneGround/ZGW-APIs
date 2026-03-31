namespace OneGround.ZGW.Zaken.DataModel.Encryption;

public interface IBsnHasher
{
    string ComputeHash(string plaintext);
}
