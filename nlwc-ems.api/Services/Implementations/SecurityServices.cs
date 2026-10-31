using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;
using nlwc.ems.Services.Options;

namespace nlwc.ems.Services.Implementations;

public class Argon2PasswordHasher : IPasswordHasher
{
    // Argon2id, 19 MiB, 2 passes, 1 lane (the OWASP minimum profile). Stored as argon2id$m$t$p$salt$hash.
    private const int Memory = 19456, Iterations = 2, Lanes = 1;

    private static readonly Lazy<string> Dummy = new(() => new Argon2PasswordHasher().Hash("dummy-password-for-timing"));

    public string DummyHash => Dummy.Value;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        return $"argon2id${Memory}${Iterations}${Lanes}${Convert.ToBase64String(salt)}${Convert.ToBase64String(Compute(password, salt, Memory, Iterations, Lanes))}";
    }

    public bool Verify(string storedHash, string password)
    {
        try
        {
            var p = storedHash.Split('$');
            var calc = Compute(password, Convert.FromBase64String(p[4]), int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]));
            return CryptographicOperations.FixedTimeEquals(calc, Convert.FromBase64String(p[5]));
        }
        catch (Exception) { return false; }
    }

    private static byte[] Compute(string password, byte[] salt, int memory, int iterations, int lanes) =>
        new Argon2id(Encoding.UTF8.GetBytes(password)) { Salt = salt, MemorySize = memory, Iterations = iterations, DegreeOfParallelism = lanes }.GetBytes(32);
}

public class TokenGenerator : ITokenGenerator
{
    public string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public class FieldEncryptor(IOptions<SecurityOptions> options) : IFieldEncryptor
{
    private readonly byte[] _key = options.Value.MasterKeyBytes();

    public byte[] Encrypt(byte[] plain)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var cipher = new byte[plain.Length];
        using var gcm = new AesGcm(_key, 16);
        gcm.Encrypt(nonce, plain, cipher, tag);
        return [.. nonce, .. tag, .. cipher];
    }

    public byte[] Decrypt(byte[] blob)
    {
        var plain = new byte[blob.Length - 28];
        using var gcm = new AesGcm(_key, 16);
        gcm.Decrypt(blob.AsSpan(0, 12), blob.AsSpan(28), blob.AsSpan(12, 16), plain);
        return plain;
    }

    public byte[] EncryptString(string plain) => Encrypt(Encoding.UTF8.GetBytes(plain));

    public string DecryptString(byte[] blob) => Encoding.UTF8.GetString(Decrypt(blob));
}

/// <summary>Placeholder until the notifications outbox and an email vendor land. Writes nothing outside development.</summary>
public class ConsoleMailer(IHostEnvironment environment, ILogger<ConsoleMailer> logger) : IMailer
{
    public Task SendAsync(string to, string template, IReadOnlyDictionary<string, string> data)
    {
        if (environment.IsDevelopment()) logger.LogInformation("[mail] {Template} -> {To} {Data}", template, to, data);
        return Task.CompletedTask;
    }
}
