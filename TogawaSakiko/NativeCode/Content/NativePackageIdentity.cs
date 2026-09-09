using System.Security.Cryptography;
using System.Text;
using TogawaSakiko.NativeCode.Bootstrap;

namespace TogawaSakiko.NativeCode.Content;

/// <summary>Exact package identity carried by the game's existing mod-list handshake.</summary>
internal static class NativePackageIdentity
{
    // Forced by ModEntryPoint.Initialize immediately after the mounted-resource check.
    private static readonly Lazy<string> Identity = new(CreateIdentity);

    internal static string HandshakeEntry => Identity.Value;

    private static string CreateIdentity()
    {
        string directory = Path.GetDirectoryName(typeof(ModEntryPoint).Assembly.Location)
            ?? throw new InvalidOperationException("Cannot locate the loaded Togawa package.");
        return CreateIdentityForDirectory(directory);
    }

    internal static string CreateIdentityForDirectory(string directory)
    {
        // Fixed order and delimiters keep the digest independent of paths and directory order.
        // PDBs are intentionally excluded: they have no runtime gameplay or resource effect.
        StringBuilder hashes = new();
        foreach (string extension in new[] { ".dll", ".json", ".pck" })
        {
            using FileStream file = File.OpenRead(Path.Combine(directory, ModEntryPoint.ModId + extension));
            hashes.Append(extension).Append(':').Append(Convert.ToHexString(SHA256.HashData(file))).Append('\n');
        }

        string digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hashes.ToString())));
        return $"{ModEntryPoint.ModId}-package-sha256-{digest}";
    }
}
