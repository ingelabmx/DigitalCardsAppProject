using System.Security.Cryptography;
using DigitalCards.Infrastructure.Branding;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DigitalCards.Web.Branding;

public sealed class BusinessLogoUploadService
{
    private const int NormalizedLogoSize = 512;
    private const string NormalizedLogoFileName = "logo.png";

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

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
        if (!string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
        {
            return new BusinessLogoUploadResult(null, "El logo debe ser PNG.");
        }

        var signature = new byte[PngSignature.Length];
        await using (var signatureInput = file.OpenReadStream())
        {
            var read = await signatureInput.ReadAsync(signature.AsMemory(0, signature.Length), cancellationToken);
            if (read < PngSignature.Length ||
                !signature.AsSpan(0, read).SequenceEqual(PngSignature))
            {
                return new BusinessLogoUploadResult(null, "El archivo no es un PNG valido.");
            }
        }

        Image<Rgba32> uploaded;
        try
        {
            await using var input = file.OpenReadStream();
            uploaded = await Image.LoadAsync<Rgba32>(input, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new BusinessLogoUploadResult(null, "El archivo PNG no se pudo procesar.");
        }

        var root = _options.GetPhysicalRoot();
        var businessFolderName = businessId.ToString("N");
        var businessFolder = Path.GetFullPath(Path.Combine(root, businessFolderName));
        EnsureChildPath(root, businessFolder);
        Directory.CreateDirectory(businessFolder);

        var versionFolderName = CreateToken();
        var versionFolder = Path.GetFullPath(Path.Combine(businessFolder, versionFolderName));
        EnsureChildPath(businessFolder, versionFolder);
        Directory.CreateDirectory(versionFolder);

        var physicalPath = Path.GetFullPath(Path.Combine(versionFolder, NormalizedLogoFileName));
        EnsureChildPath(versionFolder, physicalPath);

        using (uploaded)
        using (var normalized = NormalizeToSquare(uploaded))
        await using (var output = new FileStream(
                         physicalPath,
                         FileMode.CreateNew,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 64 * 1024,
                         useAsync: true))
        {
            await normalized.SaveAsPngAsync(output, cancellationToken);
        }

        var publicPath = $"{_options.GetRequestPath()}/{businessFolderName}/{versionFolderName}/{NormalizedLogoFileName}";
        return new BusinessLogoUploadResult(publicPath, ErrorMessage: null);
    }

    public bool IsOwned(string? publicPath)
    {
        return TryGetOwnedPhysicalPath(publicPath, out _);
    }

    public bool ExistsIfOwned(string? publicPath)
    {
        return TryGetOwnedPhysicalPath(publicPath, out var physicalPath) &&
            File.Exists(physicalPath);
    }

    public void DeleteIfOwned(string? publicPath)
    {
        if (!TryGetOwnedPhysicalPath(publicPath, out var physicalPath) ||
            !File.Exists(physicalPath))
        {
            return;
        }

        File.Delete(physicalPath);

        var versionFolder = Path.GetDirectoryName(physicalPath);
        DeleteIfEmpty(versionFolder);

        var businessFolder = versionFolder is null ? null : Path.GetDirectoryName(versionFolder);
        DeleteIfEmpty(businessFolder);
    }

    private bool TryGetOwnedPhysicalPath(string? publicPath, out string physicalPath)
    {
        physicalPath = string.Empty;
        if (string.IsNullOrWhiteSpace(publicPath))
        {
            return false;
        }

        var requestPath = _options.GetRequestPath().TrimEnd('/');
        if (!publicPath.StartsWith($"{requestPath}/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var relativePath = publicPath[(requestPath.Length + 1)..]
            .Replace('/', Path.DirectorySeparatorChar);
        var root = _options.GetPhysicalRoot();
        var resolvedPath = Path.GetFullPath(Path.Combine(root, relativePath));
        EnsureChildPath(root, resolvedPath);

        physicalPath = resolvedPath;
        return true;
    }

    private static Image<Rgba32> NormalizeToSquare(Image<Rgba32> image)
    {
        var ratio = Math.Min(
            (float)NormalizedLogoSize / image.Width,
            (float)NormalizedLogoSize / image.Height);
        var targetWidth = Math.Max(1, (int)Math.Round(image.Width * ratio));
        var targetHeight = Math.Max(1, (int)Math.Round(image.Height * ratio));

        image.Mutate(context => context.Resize(new ResizeOptions
        {
            Size = new Size(targetWidth, targetHeight),
            Mode = ResizeMode.Max
        }));

        var canvas = new Image<Rgba32>(
            NormalizedLogoSize,
            NormalizedLogoSize,
            new Rgba32(0, 0, 0, 0));
        var location = new Point(
            (NormalizedLogoSize - image.Width) / 2,
            (NormalizedLogoSize - image.Height) / 2);
        canvas.Mutate(context => context.DrawImage(image, location, opacity: 1f));
        return canvas;
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

    private static void DeleteIfEmpty(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) ||
            !Directory.Exists(directory))
        {
            return;
        }

        try
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }
        catch (IOException)
        {
            // Best effort cleanup only; stale empty folders do not affect wallet rendering.
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort cleanup only; upload ownership checks still protect file deletion.
        }
    }

    private static string CreateToken()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
