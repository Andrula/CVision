using System.Security.Cryptography;

namespace CVision.Api.Utils;

public static class FileHashHelper
{
    /// <summary>
    /// Calculates SHA256 hash of a file
    /// </summary>
    public static async Task<string> ComputeFileHashAsync(Stream fileStream)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(fileStream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Calculates SHA256 hash of an IFormFile
    /// </summary>
    public static async Task<string> ComputeFileHashAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        return await ComputeFileHashAsync(stream);
    }
}
