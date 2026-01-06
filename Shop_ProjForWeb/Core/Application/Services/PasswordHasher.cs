using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Shop_ProjForWeb.Core.Application.Services;

public static class PasswordHasher
{
    // PBKDF2 settings
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32;  // 256 bit
    private const int Iterations = 10000;

    public static string HashPassword(string password)
    {
        var salt = new byte[SaltSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);

        var derived = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, Iterations, KeySize);

        var result = new byte[SaltSize + KeySize];
        Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
        Buffer.BlockCopy(derived, 0, result, SaltSize, KeySize);

        return Convert.ToBase64String(result);
    }

    public static bool VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        try
        {
            var bytes = Convert.FromBase64String(hashedPassword);
            var salt = new byte[SaltSize];
            Buffer.BlockCopy(bytes, 0, salt, 0, SaltSize);
            var storedKey = new byte[KeySize];
            Buffer.BlockCopy(bytes, SaltSize, storedKey, 0, KeySize);

            var derived = KeyDerivation.Pbkdf2(providedPassword, salt, KeyDerivationPrf.HMACSHA256, Iterations, KeySize);

            return CryptographicOperations.FixedTimeEquals(storedKey, derived);
        }
        catch
        {
            return false;
        }
    }
}
