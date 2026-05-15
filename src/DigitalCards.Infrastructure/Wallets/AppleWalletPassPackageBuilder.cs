using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using DigitalCards.Application.Models;
using DigitalCards.Domain;
using DigitalCards.Infrastructure.Branding;
using Microsoft.Extensions.Options;

namespace DigitalCards.Infrastructure.Wallets;

public sealed class AppleWalletPassPackageBuilder
{
    public const string ContentType = "application/vnd.apple.pkpass";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static readonly HashSet<string> ManifestExcludedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "manifest.json",
        "signature"
    };

    private static readonly HashSet<string> AssetExcludedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "pass.json",
        "manifest.json",
        "signature"
    };

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly BusinessLogoUploadOptions _logoUploadOptions;

    public AppleWalletPassPackageBuilder()
        : this(Options.Create(new BusinessLogoUploadOptions()))
    {
    }

    public AppleWalletPassPackageBuilder(IOptions<BusinessLogoUploadOptions> logoUploadOptions)
    {
        _logoUploadOptions = logoUploadOptions.Value;
    }

    public AppleWalletPassFile Build(
        LoyaltyCard card,
        Client client,
        Business business,
        AppleWalletOptions options,
        AppleWalletPassBuildSettings? settings = null)
    {
        var serialNumber = CreateSerialNumber(card);
        var files = BuildUnsignedFiles(card, client, business, options, serialNumber, settings);
        var manifest = BuildManifest(files);
        var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions);
        files["manifest.json"] = manifestBytes;
        files["signature"] = SignManifest(manifestBytes, options);

        return new AppleWalletPassFile(
            BuildZip(files),
            ContentType,
            $"{serialNumber}.pkpass",
            serialNumber,
            DateTimeOffset.UtcNow);
    }

    public byte[] BuildPassJson(
        LoyaltyCard card,
        Client client,
        Business business,
        AppleWalletOptions options,
        string? serialNumber = null,
        AppleWalletPassBuildSettings? settings = null)
    {
        Require(options.TeamIdentifier, "DigitalCards:AppleWallet:TeamIdentifier");
        Require(options.PassTypeIdentifier, "DigitalCards:AppleWallet:PassTypeIdentifier");
        Require(options.OrganizationName, "DigitalCards:AppleWallet:OrganizationName");

        var pass = new
        {
            formatVersion = 1,
            passTypeIdentifier = options.PassTypeIdentifier,
            serialNumber = serialNumber ?? CreateSerialNumber(card),
            teamIdentifier = options.TeamIdentifier,
            organizationName = options.OrganizationName,
            description = business.DisplayName,
            webServiceURL = settings?.WebServiceUrl,
            authenticationToken = settings?.AuthenticationToken,
            logoText = business.ProgramName ?? business.DisplayName,
            foregroundColor = business.CustomFieldColor ?? options.ForegroundColor,
            backgroundColor = business.PrimaryColor ?? options.BackgroundColor,
            labelColor = business.SecondaryColor ?? options.LabelColor,
            barcodes = new[]
            {
                new
                {
                    message = client.UserName,
                    format = "PKBarcodeFormatQR",
                    messageEncoding = "iso-8859-1",
                    altText = client.UserName
                }
            },
            barcode = new
            {
                message = client.UserName,
                format = "PKBarcodeFormatQR",
                messageEncoding = "iso-8859-1",
                altText = client.UserName
            },
            generic = new
            {
                primaryFields = new[]
                {
                    Field("businessName", "Nombre del negocio", business.DisplayName)
                },
                secondaryFields = new[]
                {
                    Field("client", "Cliente", ShortClientName(client)),
                    Field(
                        "currentStamps",
                        "Sellos",
                        FormatStampProgress(card.CurrentStamps, business.StampGoal),
                        "Sellos actualizados: %@"),
                    Field("reward", "Recompensa", business.ProgramDescription ?? options.Description ?? string.Empty)
                },
                backFields = new[]
                {
                    Field("username", "Usuario", client.UserName),
                    Field("terms", "Actualizaciones", "Esta version inicial se actualiza al descargar una nueva tarjeta.")
                }
            }
        };

        return JsonSerializer.SerializeToUtf8Bytes(pass, JsonOptions);
    }

    private static string FormatStampProgress(int currentStamps, int stampGoal)
    {
        var goal = stampGoal > 0 ? stampGoal : Business.DefaultStampGoal;
        var current = Math.Min(Math.Max(currentStamps, 0), goal);
        return $"{current.ToString(CultureInfo.InvariantCulture)} de {goal.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string ShortClientName(Client client)
    {
        var firstName = FirstToken(client.FirstName);
        var lastName = FirstToken(client.LastName);
        var displayName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(displayName) ? client.UserName : displayName;
    }

    private static string FirstToken(string value)
    {
        return value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
    }

    public IReadOnlyDictionary<string, string> BuildManifest(IReadOnlyDictionary<string, byte[]> files)
    {
        return files
            .Where(file => !ManifestExcludedNames.Contains(file.Key))
            .OrderBy(file => file.Key, StringComparer.Ordinal)
            .ToDictionary(file => file.Key, file => Sha1Hex(file.Value), StringComparer.Ordinal);
    }

    private Dictionary<string, byte[]> BuildUnsignedFiles(
        LoyaltyCard card,
        Client client,
        Business business,
        AppleWalletOptions options,
        string serialNumber,
        AppleWalletPassBuildSettings? settings)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["pass.json"] = BuildPassJson(card, client, business, options, serialNumber, settings)
        };

        foreach (var asset in LoadAssets(options))
        {
            files[asset.Key] = asset.Value;
        }

        var hasManagedLogoPath = IsManagedUploadedLogoPath(business.LogoPath);
        if (hasManagedLogoPath)
        {
            files.Remove("logo.png");
            files.Remove("logo@2x.png");
        }

        if (TryLoadUploadedLogoPng(business.LogoPath) is { } logoBytes)
        {
            files["logo.png"] = logoBytes;
            files["logo@2x.png"] = logoBytes;
        }

        return files;
    }

    private bool IsManagedUploadedLogoPath(string? logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath))
        {
            return false;
        }

        var requestPath = _logoUploadOptions.GetRequestPath();
        return logoPath.StartsWith($"{requestPath}/", StringComparison.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, byte[]> BuildUnsignedFiles(
        LoyaltyCard card,
        Client client,
        Business business,
        AppleWalletOptions options,
        AppleWalletPassBuildSettings? settings = null)
    {
        return BuildUnsignedFiles(card, client, business, options, CreateSerialNumber(card), settings);
    }

    private static IReadOnlyDictionary<string, byte[]> LoadAssets(AppleWalletOptions options)
    {
        Require(options.AssetsPath, "DigitalCards:AppleWallet:AssetsPath");

        if (!Directory.Exists(options.AssetsPath))
        {
            throw new InvalidOperationException("The configured Apple Wallet assets directory was not found.");
        }

        var root = Path.GetFullPath(options.AssetsPath);
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(file => !File.GetAttributes(file).HasFlag(FileAttributes.Hidden))
            .Select(file => new
            {
                Path = file,
                RelativePath = Path.GetRelativePath(root, file).Replace('\\', '/')
            })
            .Where(file => !AssetExcludedNames.Contains(file.RelativePath))
            .ToDictionary(file => file.RelativePath, file => File.ReadAllBytes(file.Path), StringComparer.Ordinal);

        if (!files.ContainsKey("icon.png"))
        {
            throw new InvalidOperationException("DigitalCards:AppleWallet:AssetsPath must contain icon.png.");
        }

        return files;
    }

    private byte[]? TryLoadUploadedLogoPng(string? logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath) ||
            !logoPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var requestPath = _logoUploadOptions.GetRequestPath();
        if (!logoPath.StartsWith($"{requestPath}/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relativePath = logoPath[(requestPath.Length + 1)..]
            .Replace('/', Path.DirectorySeparatorChar);
        var root = _logoUploadOptions.GetPhysicalRoot();
        var physicalPath = Path.GetFullPath(Path.Combine(root, relativePath));
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        if (!physicalPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(physicalPath))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(physicalPath);
        return bytes.Length >= PngSignature.Length &&
            bytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature)
            ? bytes
            : null;
    }

    private static byte[] SignManifest(byte[] manifestBytes, AppleWalletOptions options)
    {
        Require(options.CertificatePath, "DigitalCards:AppleWallet:CertificatePath");
        Require(options.CertificatePassword, "DigitalCards:AppleWallet:CertificatePassword");
        Require(options.WwdrCertificatePath, "DigitalCards:AppleWallet:WwdrCertificatePath");

        if (!File.Exists(options.CertificatePath))
        {
            throw new InvalidOperationException("The configured Apple Wallet signing certificate was not found.");
        }

        if (!File.Exists(options.WwdrCertificatePath))
        {
            throw new InvalidOperationException("The configured Apple WWDR certificate was not found.");
        }

        var signingCertificate = new X509Certificate2(
            options.CertificatePath,
            options.CertificatePassword,
            X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);

        if (!signingCertificate.HasPrivateKey)
        {
            throw new InvalidOperationException("The configured Apple Wallet signing certificate must include a private key.");
        }

        var wwdrCertificate = new X509Certificate2(options.WwdrCertificatePath);
        if (!wwdrCertificate.Subject.Contains("Apple Worldwide Developer Relations Certification Authority", StringComparison.OrdinalIgnoreCase) ||
            !wwdrCertificate.Subject.Contains("O=Apple Inc.", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("DigitalCards:AppleWallet:WwdrCertificatePath must point to the Apple WWDR G4 intermediate certificate, not the Pass Type ID certificate.");
        }

        var contentInfo = new ContentInfo(manifestBytes);
        var signedCms = new SignedCms(contentInfo, detached: true);
        var signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, signingCertificate)
        {
            DigestAlgorithm = new Oid("1.3.14.3.2.26"),
            IncludeOption = X509IncludeOption.ExcludeRoot
        };
        signer.SignedAttributes.Add(new Pkcs9SigningTime(DateTimeOffset.UtcNow.UtcDateTime));
        signer.Certificates.Add(wwdrCertificate);

        signedCms.ComputeSignature(signer);
        return signedCms.Encode();
    }

    private static byte[] BuildZip(IReadOnlyDictionary<string, byte[]> files)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files.OrderBy(file => file.Key, StringComparer.Ordinal))
            {
                var entry = archive.CreateEntry(file.Key, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                entryStream.Write(file.Value, 0, file.Value.Length);
            }
        }

        return stream.ToArray();
    }

    private static string CreateSerialNumber(LoyaltyCard card)
    {
        return card.Id.ToString("N");
    }

    private static object Field(string key, string label, string value, string? changeMessage = null)
    {
        return new
        {
            key,
            label,
            value,
            changeMessage
        };
    }

    private static string Sha1Hex(byte[] bytes)
    {
        return Convert.ToHexString(SHA1.HashData(bytes)).ToLowerInvariant();
    }

    private static void Require(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is required when real Apple Wallet is enabled.");
        }
    }

    public sealed record AppleWalletPassBuildSettings(
        string WebServiceUrl,
        string AuthenticationToken);
}
