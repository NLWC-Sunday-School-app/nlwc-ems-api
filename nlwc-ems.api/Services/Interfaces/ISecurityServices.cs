namespace nlwc.ems.Services.Interfaces;

/// <summary>Argon2id password hashing.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string storedHash, string password);

    /// <summary>A hash to verify against when the account does not exist, so response time does not reveal it.</summary>
    string DummyHash { get; }
}

/// <summary>Opaque random tokens (sessions, verification links). Only the SHA-256 hash is ever stored.</summary>
public interface ITokenGenerator
{
    string NewToken();
    string Hash(string token);
}

/// <summary>AES-256-GCM encryption for secrets and phone numbers at rest, keyed by <c>Security:MasterKey</c>.</summary>
public interface IFieldEncryptor
{
    byte[] Encrypt(byte[] plain);
    byte[] Decrypt(byte[] blob);
    byte[] EncryptString(string plain);
    string DecryptString(byte[] blob);
}

public interface IMailer
{
    Task SendAsync(string to, string template, IReadOnlyDictionary<string, string> data);
}
