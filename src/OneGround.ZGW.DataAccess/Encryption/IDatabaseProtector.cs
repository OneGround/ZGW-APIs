namespace OneGround.ZGW.DataAccess.Encryption;

public interface IDatabaseProtector
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}

public interface IDatabaseProtector<TContext> : IDatabaseProtector
    where TContext : class { }
