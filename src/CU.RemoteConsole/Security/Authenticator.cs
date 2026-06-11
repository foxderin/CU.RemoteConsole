using System;
using System.Security.Cryptography;
using System.Text;

namespace CU.RemoteConsole.Security;

public sealed class Authenticator
{
    private readonly string token;
    private readonly byte[] tokenBytes;

    public Authenticator(string token)
    {
        this.token = token ?? string.Empty;
        tokenBytes = Encoding.UTF8.GetBytes(this.token);
    }

    public bool Validate(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return false;
        }

        const string prefix = "Bearer ";
        if (!authorizationHeader.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var candidate = authorizationHeader.Substring(prefix.Length).Trim();
        var candidateBytes = Encoding.UTF8.GetBytes(candidate);
        return FixedTimeEquals(tokenBytes, candidateBytes);
    }

    public string Fingerprint(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return "missing";
        }

        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(authorizationHeader));
            return BitConverter.ToString(hash, 0, 6).Replace("-", "").ToLowerInvariant();
        }
    }

    private static bool FixedTimeEquals(byte[] expected, byte[] actual)
    {
        var diff = expected.Length ^ actual.Length;
        var length = Math.Min(expected.Length, actual.Length);

        for (var i = 0; i < length; i++)
        {
            diff |= expected[i] ^ actual[i];
        }

        return diff == 0;
    }
}
