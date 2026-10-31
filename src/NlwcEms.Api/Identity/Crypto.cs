using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace NlwcEms.Api.Identity;

public static class Crypto
{
    public static Guid NewId() => Guid.CreateVersion7();

    /// <summary>Opaque random token for sessions, claims and verification. Only the hash is stored.</summary>
    public static string NewToken() => Base64Url(RandomNumberGenerator.GetBytes(32));
    public static string HashToken(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64Url(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // Argon2id, 19 MiB, 2 passes, 1 lane (OWASP minimum profile). Stored as argon2id$m$t$p$salt$hash.
    private const int Mem = 19456, Iter = 2, Lanes = 1;

    private static byte[] Argon(string pw, byte[] salt, int mem, int iter, int lanes) =>
        new Argon2id(Encoding.UTF8.GetBytes(pw)) { Salt = salt, MemorySize = mem, Iterations = iter, DegreeOfParallelism = lanes }.GetBytes(32);

    public static string HashPassword(string pw)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        return $"argon2id${Mem}${Iter}${Lanes}${Convert.ToBase64String(salt)}${Convert.ToBase64String(Argon(pw, salt, Mem, Iter, Lanes))}";
    }

    public static bool VerifyPassword(string stored, string pw)
    {
        try
        {
            var p = stored.Split('$');
            var calc = Argon(pw, Convert.FromBase64String(p[4]), int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]));
            return CryptographicOperations.FixedTimeEquals(calc, Convert.FromBase64String(p[5]));
        }
        catch (Exception) { return false; }
    }

    /// <summary>Verified against when the account does not exist, so response time does not reveal it.</summary>
    public static readonly string DummyHash = HashPassword("dummy-password-for-timing");

    public static byte[] Encrypt(byte[] key, byte[] plain)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var cipher = new byte[plain.Length];
        using var gcm = new AesGcm(key, 16);
        gcm.Encrypt(nonce, plain, cipher, tag);
        return [.. nonce, .. tag, .. cipher];
    }

    public static byte[] Decrypt(byte[] key, byte[] blob)
    {
        var plain = new byte[blob.Length - 28];
        using var gcm = new AesGcm(key, 16);
        gcm.Decrypt(blob.AsSpan(0, 12), blob.AsSpan(28), blob.AsSpan(12, 16), plain);
        return plain;
    }
}
