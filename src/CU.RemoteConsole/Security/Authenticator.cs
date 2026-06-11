using System;
using System.Security.Cryptography;
using System.Text;

namespace CU.RemoteConsole.Security;

public sealed class Authenticator
{
    private readonly object gate = new object();
    private string token = string.Empty;
    private byte[] tokenBytes = Array.Empty<byte>();

    public Authenticator(string token)
    {
        UpdateToken(token);
    }

    public void UpdateToken(string token)
    {
        lock (gate)
        {
            this.token = token ?? string.Empty;
            tokenBytes = Encoding.UTF8.GetBytes(this.token);
        }
    }

    public bool Validate(string? authorizationHeader)
    {
        lock (gate)
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
