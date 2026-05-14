using System.Security.Cryptography;
using DigitalCards.Infrastructure.Branding;
using Microsoft.Extensions.Options;

namespace DigitalCards.Web.Branding;

public sealed class BusinessLogoUploadService
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    private readonly BusinessLogoUploadOptions _options;

    public BusinessLogoUploadService(IOptions<BusinessLogoUploadOptions> options)
    {
        _options = options.Value;
    }

    public async Task<BusinessLogoUploadResult> SaveAsync(
        Guid businessId,
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return new BusinessLogoUploadResult(null, "Selecciona un archivo de logo.");
        }

        if (file.Length > _options.MaxBytes)
        {
            return new BusinessLogoUploadResult(null, "El logo no puede exceder 2 MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return new BusinessLogoUploadResult(null, "El logo debe ser PNG, JPG, JPEG o WebP.");
        }

        var signature = new byte[12];
        await using (var signatureInput = file.OpenReadStream())
        {
            var read = await signatureInput.ReadAsync(signature.AsMemory(0, signature.Length), cancellationToken);
            if (!MatchesExtension(signature.AsSpan(0, read), extension))
            {
                return new BusinessLogoUploadResult(null, "El archivo no coincide con un formato de logo permitido.");
            }
        }

        await using var input = file.OpenReadStream();
        if (input.CanSeek)
        {
            input.Position = 0;
        }
        var root = _options.GetPhysicalRoot();
        var businessFolderName = businessId.ToString("N");
        var businessFolder = Path.GetFullPath(Path.Combine(root, businessFolderName));
        EnsureChildPath(root, businessFolder);
        Directory.CreateDirectory(businessFolder);

        var fileName = $"{CreateToken()}{extension}";
        var physicalPath = Path.GetFullPath(Path.Combine(businessFolder, fileName));
        EnsureChildPath(businessFolder, physicalPath);

        await using (var output = new FileStream(
            physicalPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true))
        {
            await input.CopyToAsync(output, cancellationToken);
        }

        var publicPath = $"{_options.GetRequestPath()}/{businessFolderName}/{fileName}";
        return new BusinessLogoUploadResult(publicPath, ErrorMessage: null);
    }

    public void DeleteIfOwned(string? publicPath)
    {
        if (string.IsNullOrWhiteSpace(publicPath))
        {
            return;
        }

        var requestPath = _options.GetRequestPath().TrimEnd('/');
        if (!publicPath.StartsWith($"{requestPath}/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var relativePath = publicPath[(requestPath.Length + 1)..]
            .Replace('/', Path.DirectorySeparatorChar);
        var root = _options.GetPhysicalRoot();
        var physicalPath = Path.GetFullPath(Path.Combine(root, relativePath));
        EnsureChildPath(root, physicalPath);

        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }
    }

    private static bool MatchesExtension(ReadOnlySpan<byte> signature, string extension)
    {
        return extension switch
        {
            ".png" => signature.Length >= PngSignature.Length &&
                signature[..PngSignature.Length].SequenceEqual(PngSignature),
            ".jpg" or ".jpeg" => signature.Length >= 3 &&
                signature[0] == 0xFF &&
                signature[1] == 0xD8 &&
                signature[2] == 0xFF,
            ".webp" => signature.Length >= 12 &&
                signature[..4].SequenceEqual("RIFF"u8) &&
                signature[8..12].SequenceEqual("WEBP"u8),
            _ => false
        };
    }

    private static void EnsureChildPath(string root, string path)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        var normalizedPath = Path.GetFullPath(path);

        if (!normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The logo upload path is invalid.");
        }
    }

    private static string CreateToken()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
