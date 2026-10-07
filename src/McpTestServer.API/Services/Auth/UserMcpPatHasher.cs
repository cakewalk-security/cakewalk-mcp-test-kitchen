using System.Security.Cryptography;
using System.Text;

namespace McpTestServer.API.Services.Auth;

internal static class UserMcpPatHasher
{
    public static string Hash(string pat) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pat)));
}
