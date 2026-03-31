namespace OneGround.ZGW.Zaken.DataModel.Encryption;

public interface IDatabaseProtector
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}
