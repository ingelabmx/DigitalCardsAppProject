using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Walletobjects.v1;
using Google.Apis.Walletobjects.v1.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DigitalCards.Infrastructure.Wallets;

public sealed class GoogleWalletService : IGoogleWalletService
{
    private static readonly JsonSerializerSettings ExcludeNulls = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly ILogger<GoogleWalletService> _logger;
    private readonly GoogleWalletOptions _options;

    public GoogleWalletService(
        IOptions<GoogleWalletOptions> options,
        ILogger<GoogleWalletService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GoogleWalletIssueResult> IssueSaveLinkAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken = default)
    {
        var walletClient = CreateWalletClient();
        var classSuffix = NormalizeSuffix(business.GoogleClassSuffix, "business class suffix");
        var objectSuffix = GetObjectSuffix(card);
        var classId = BuildFullId(classSuffix);
        var objectId = BuildFullId(objectSuffix);

        await EnsureClassAsync(walletClient.Service, classId, cancellationToken);
        await EnsureObjectAsync(walletClient.Service, objectId, classId, card, client, business, cancellationToken);

        var saveUrl = CreateSaveUrl(walletClient.Credentials, classId, objectId);
        _logger.LogInformation("Issued Google Wallet save URL for object {ObjectId}.", objectId);

        return new GoogleWalletIssueResult(objectSuffix, saveUrl);
    }

    public async Task PatchStampStateAsync(
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(card.GoogleObjectId))
        {
            return;
        }

        var walletClient = CreateWalletClient();
        var objectSuffix = NormalizeSuffix(card.GoogleObjectId, "Google object suffix");
        var objectId = BuildFullId(objectSuffix);

        var patchBody = new GenericObject
        {
            TextModulesData = BuildTextModules(card)
        };

        try
        {
            await walletClient.Service.Genericobject.Patch(patchBody, objectId).ExecuteAsync(cancellationToken);
            _logger.LogInformation(
                "Patched Google Wallet object {ObjectId} with {CurrentStamps} current stamps.",
                objectId,
                card.CurrentStamps);
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                exception,
                "Google Wallet object {ObjectId} was missing during patch. Recreating it.",
                objectId);

            var classSuffix = NormalizeSuffix(business.GoogleClassSuffix, "business class suffix");
            var classId = BuildFullId(classSuffix);
            await EnsureClassAsync(walletClient.Service, classId, cancellationToken);
            await EnsureObjectAsync(walletClient.Service, objectId, classId, card, client, business, cancellationToken);
        }
    }

    private WalletClient CreateWalletClient()
    {
        if (string.IsNullOrWhiteSpace(_options.IssuerId))
        {
            throw new InvalidOperationException("DigitalCards:GoogleWallet:IssuerId is required when real Google Wallet is enabled.");
        }

        if (_options.Origins.Length == 0 || _options.Origins.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("DigitalCards:GoogleWallet:Origins must contain at least one origin when real Google Wallet is enabled.");
        }

        var credentialsFilePath = _options.CredentialsFilePath;
        if (string.IsNullOrWhiteSpace(credentialsFilePath))
        {
            throw new InvalidOperationException("DigitalCards:GoogleWallet:CredentialsFilePath is required when real Google Wallet is enabled.");
        }

        if (!File.Exists(credentialsFilePath))
        {
            throw new InvalidOperationException("The configured Google Wallet credentials file was not found.");
        }

        var serviceAccountCredential = CredentialFactory.FromFile<ServiceAccountCredential>(credentialsFilePath);
        var googleCredential = serviceAccountCredential
            .ToGoogleCredential()
            .CreateScoped(WalletobjectsService.ScopeConstants.WalletObjectIssuer);

        var service = new WalletobjectsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = googleCredential,
            ApplicationName = _options.ApplicationName
        });

        return new WalletClient(serviceAccountCredential, service);
    }

    private async Task EnsureClassAsync(
        WalletobjectsService service,
        string classId,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.Genericclass.Get(classId).ExecuteAsync(cancellationToken);
            return;
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode == HttpStatusCode.NotFound)
        {
        }

        var newClass = new GenericClass
        {
            Id = classId,
            MultipleDevicesAndHoldersAllowedStatus = "ONE_USER_ALL_DEVICES",
            ClassTemplateInfo = new ClassTemplateInfo
            {
                CardTemplateOverride = new CardTemplateOverride
                {
                    CardRowTemplateInfos =
                    [
                        new CardRowTemplateInfo
                        {
                            TwoItems = new CardRowTwoItems
                            {
                                StartItem = new TemplateItem
                                {
                                    FirstValue = new FieldSelector
                                    {
                                        Fields =
                                        [
                                            new FieldReference
                                            {
                                                FieldPath = "object.textModulesData['checks']"
                                            }
                                        ]
                                    }
                                },
                                EndItem = new TemplateItem
                                {
                                    FirstValue = new FieldSelector
                                    {
                                        Fields =
                                        [
                                            new FieldReference
                                            {
                                                FieldPath = "object.textModulesData['dateCreated']"
                                            }
                                        ]
                                    }
                                }
                            }
                        }
                    ]
                }
            }
        };

        try
        {
            await service.Genericclass.Insert(newClass).ExecuteAsync(cancellationToken);
            _logger.LogInformation("Created Google Wallet class {ClassId}.", classId);
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogInformation("Google Wallet class {ClassId} already exists.", classId);
        }
    }

    private async Task EnsureObjectAsync(
        WalletobjectsService service,
        string objectId,
        string classId,
        LoyaltyCard card,
        Client client,
        Business business,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.Genericobject.Get(objectId).ExecuteAsync(cancellationToken);
            return;
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode == HttpStatusCode.NotFound)
        {
        }

        var newObject = BuildObject(objectId, classId, card, client, business);

        try
        {
            await service.Genericobject.Insert(newObject).ExecuteAsync(cancellationToken);
            _logger.LogInformation("Created Google Wallet object {ObjectId}.", objectId);
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogInformation("Google Wallet object {ObjectId} already exists.", objectId);
        }
    }

    private GenericObject BuildObject(
        string objectId,
        string classId,
        LoyaltyCard card,
        Client client,
        Business business)
    {
        var genericObject = new GenericObject
        {
            Id = objectId,
            ClassId = classId,
            State = "ACTIVE",
            TextModulesData = BuildTextModules(card),
            Barcode = new Barcode
            {
                Type = "QR_CODE",
                Value = client.UserName
            },
            CardTitle = Localized(GetCardTitle(business)),
            Header = Localized(client.FullName),
            Subheader = Localized("Titular"),
            HexBackgroundColor = business.PrimaryColor ?? _options.HexBackgroundColor
        };

        if (!string.IsNullOrWhiteSpace(_options.HeroImageUri))
        {
            genericObject.HeroImage = Image(_options.HeroImageUri, "Hero image");
        }

        if (!string.IsNullOrWhiteSpace(_options.LogoImageUri))
        {
            genericObject.Logo = Image(_options.LogoImageUri, "Card logo");
        }

        return genericObject;
    }

    private IList<TextModuleData> BuildTextModules(LoyaltyCard card)
    {
        return
        [
            new TextModuleData
            {
                Id = "checks",
                Header = "Sellos",
                Body = card.CurrentStamps.ToString(CultureInfo.InvariantCulture)
            },
            new TextModuleData
            {
                Id = "totalchecks",
                Header = "Sellos historicos",
                Body = card.LifetimeStamps.ToString(CultureInfo.InvariantCulture)
            },
            new TextModuleData
            {
                Id = "dateCreated",
                Header = "Fecha de alta",
                Body = card.CreatedAt.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            }
        ];
    }

    private string CreateSaveUrl(
        ServiceAccountCredential credentials,
        string classId,
        string objectId)
    {
        var objectsToAdd = new
        {
            genericObjects = new List<GenericObject>
            {
                new()
                {
                    Id = objectId,
                    ClassId = classId
                }
            }
        };

        var serializedPayload = JObject.Parse(JsonConvert.SerializeObject(objectsToAdd, ExcludeNulls));
        var jwtPayload = JObject.Parse(JsonConvert.SerializeObject(new
        {
            iss = credentials.Id,
            aud = "google",
            origins = _options.Origins,
            typ = "savetowallet",
            payload = serializedPayload
        }, ExcludeNulls));

        var claims = JwtPayload.Deserialize(jwtPayload.ToString(Formatting.None));
        var key = new RsaSecurityKey(credentials.Key);
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        var jwt = new JwtSecurityToken(new JwtHeader(signingCredentials), claims);
        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        return $"https://pay.google.com/gp/v/save/{token}";
    }

    private string GetObjectSuffix(LoyaltyCard card)
    {
        if (!string.IsNullOrWhiteSpace(card.GoogleObjectId))
        {
            return NormalizeSuffix(card.GoogleObjectId, "Google object suffix");
        }

        return card.Id.ToString("N")[^10..];
    }

    private string BuildFullId(string suffix)
    {
        return $"{_options.IssuerId}.{suffix}";
    }

    private string NormalizeSuffix(string value, string name)
    {
        var normalized = new string(value.Where(character =>
            char.IsLetterOrDigit(character) ||
            character == '_' ||
            character == '-' ||
            character == '.').ToArray());

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException($"{name} is required for Google Wallet.");
        }

        return normalized;
    }

    private Image Image(string uri, string description)
    {
        return new Image
        {
            SourceUri = new ImageUri
            {
                Uri = uri
            },
            ContentDescription = Localized(description)
        };
    }

    private LocalizedString Localized(string value)
    {
        return new LocalizedString
        {
            DefaultValue = new TranslatedString
            {
                Language = _options.Language,
                Value = value
            }
        };
    }

    private static string GetCardTitle(Business business)
    {
        return business.ProgramName ?? business.DisplayName;
    }

    private sealed record WalletClient(
        ServiceAccountCredential Credentials,
        WalletobjectsService Service);
}
