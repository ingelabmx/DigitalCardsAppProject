using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Walletobjects.v1;
using Google.Apis.Walletobjects.v1.Data;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Web;

class Loyalty
{
    private string issuerID = "3388000000022809693";

    public static string keyFilePath;

    public static ServiceAccountCredential credentials;

    public static WalletobjectsService service;

    public Loyalty()
    {
        keyFilePath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (string.IsNullOrWhiteSpace(keyFilePath))
        {
            throw new InvalidOperationException("GOOGLE_APPLICATION_CREDENTIALS must point to a Google Wallet service account JSON file outside source control.");
        }

        Auth();
    }

    public void Auth()
    {
        credentials = (ServiceAccountCredential)GoogleCredential
            .FromFile(keyFilePath)
            .CreateScoped(new List<string>
            {WalletobjectsService.ScopeConstants.WalletObjectIssuer})
            .UnderlyingCredential;

        service = new WalletobjectsService(
            new BaseClientService.Initializer()
            {
                HttpClientInitializer = credentials
            });
    }

    public string CreateClass(string classSuffix)
    {
        // Check if the class exists
        Stream responseStream = service.Genericclass
            .Get($"{issuerID}.{classSuffix}")
            .ExecuteAsStream();

        StreamReader responseReader = new StreamReader(responseStream);
        JObject jsonResponse = JObject.Parse(responseReader.ReadToEnd());

        if (!jsonResponse.ContainsKey("error"))
        {
            Console.WriteLine($"Class {issuerID}.{classSuffix} already exists!");
            return $"{issuerID}.{classSuffix}";
        }
        else if (jsonResponse["error"].Value<int>("code") != 404)
        {
            // Something else went wrong...
            Console.WriteLine(jsonResponse.ToString());
            return $"{issuerID}.{classSuffix}";
        }

        // See link below for more information on required properties
        // https://developers.google.com/wallet/generic/rest/v1/genericclass
        GenericClass newClass = new GenericClass
        {
            Id = $"{issuerID}.{classSuffix}",
            MultipleDevicesAndHoldersAllowedStatus = "ONE_USER_ALL_DEVICES",
            ClassTemplateInfo = new ClassTemplateInfo
            {
                CardTemplateOverride = new CardTemplateOverride
                {
                    CardRowTemplateInfos = new List<CardRowTemplateInfo>
                    {
                        new CardRowTemplateInfo
                        {
                            TwoItems = new CardRowTwoItems
                            {
                                StartItem = new TemplateItem
                                {
                                    FirstValue = new FieldSelector
                                    {
                                        Fields = new List<FieldReference>
                                        {
                                            new FieldReference
                                            {
                                                FieldPath = "object.textModulesData['checks']"
                                            }
                                        }
                                    }
                                },
                                EndItem = new TemplateItem
                                {
                                    FirstValue = new FieldSelector
                                    {
                                        Fields = new List<FieldReference>
                                        {
                                            new FieldReference
                                            {
                                                FieldPath = "object.textModulesData['dateCreated']"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        responseStream = service.Genericclass
            .Insert(newClass)
            .ExecuteAsStream();

        responseReader = new StreamReader(responseStream);
        jsonResponse = JObject.Parse(responseReader.ReadToEnd());

        Console.WriteLine("Class insert response");
        Console.WriteLine(jsonResponse.ToString());

        return "Success";
    }

    public string CreateObject(string objectSuffix, string classSuffix, string businessName, string userName, string firstName, string lastName, string checks, string totalchecks, string dateCreated)
    {
        TextFormats text = new TextFormats();

        // Check if the object exists
        Stream responseStream = service.Loyaltyobject.Get($"{issuerID}.{objectSuffix}").ExecuteAsStream();

        StreamReader responseReader = new StreamReader(responseStream);
        JObject jsonResponse = JObject.Parse(responseReader.ReadToEnd());

        if (!jsonResponse.ContainsKey("error"))
        {
            Console.WriteLine($"Object {issuerID}.{objectSuffix} already exists!");
            return $"{issuerID}.{objectSuffix}";
        }
        else if (jsonResponse["error"].Value<int>("code") != 404)
        {
            // Somethig else went wrong
            Console.WriteLine(jsonResponse.ToString());
            return $"{issuerID}.{objectSuffix}";
        }


        GenericObject newObject = new GenericObject
        {
            Id = $"{issuerID}.{objectSuffix}",
            ClassId = $"{issuerID}.{classSuffix}",
            State = "ACTIVE",
            HeroImage = new Image
            {
                SourceUri = new ImageUri
                {
                    Uri = "https://drive.google.com/uc?export=view&id=1AQ6mabyWRiBf58gNTu2C6mEsb_863z5u"
                },
                ContentDescription = new LocalizedString
                {
                    DefaultValue = new TranslatedString
                    {
                        Language = "en-US",
                        Value = "Hero image"
                    }
                }
            },
            TextModulesData = new List<TextModuleData>
            {
                new TextModuleData
                {
                  Header = "Checks",
                  Body = checks,
                  Id = "checks"
                },
                new TextModuleData
                {
                  Header = "Total Checks",
                  Body = totalchecks,
                  Id = "totalchecks"
                },
                new TextModuleData
                {
                    Header = "Date Created",
                  Body = dateCreated,
                  Id = "dateCreated"
                }
            },
            Barcode = new Barcode
            {
                Type = "QR_CODE",
                Value = userName

            },
            CardTitle = new LocalizedString
            {
                DefaultValue = new TranslatedString
                {
                    Language = "en-US",
                    Value = businessName

                }
            },
            Header = new LocalizedString
            {
                DefaultValue = new TranslatedString
                {
                    Language = "en-US",
                    Value = firstName + " " + lastName
                }
            },
            Subheader = new LocalizedString
            {
                DefaultValue = new TranslatedString
                {
                    Language = "en-US",
                    Value = "Titular"
                }
            }
            ,
            HexBackgroundColor = "#ACD2E8",
            // Colocar el logo de la pagina

            Logo = new Image
            {
                SourceUri = new ImageUri
                {
                    //Uri = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + "/Resources/Logo.jpg"
                    Uri = "https://drive.usercontent.google.com/download?id=1UV3C6c4vzWbnHwsiLp0tBAryx7PBu8K6"
                },
                ContentDescription = new LocalizedString
                {
                    DefaultValue = new TranslatedString
                    {
                        Language = "en-US",
                        Value = "Page card logo"
                    }
                },
            }
        };

        responseStream = service.Genericobject
            .Insert(newObject)
            .ExecuteAsStream();
        responseReader = new StreamReader(responseStream);
        jsonResponse = JObject.Parse(responseReader.ReadToEnd());

        Console.WriteLine("Object insert response");
        Console.WriteLine(jsonResponse.ToString());

        return "Success";

    }

    public string PatchObject(string objectSuffix, string checks, string totalchecks)
    {
        // Check if the object exists
        Stream responseStream = service.Genericobject
            .Get($"{issuerID}.{objectSuffix}")
            .ExecuteAsStream();

        StreamReader responseReader = new StreamReader(responseStream);
        JObject jsonResponse = JObject.Parse(responseReader.ReadToEnd());

        if (jsonResponse.ContainsKey("error"))
        {
            if (jsonResponse["error"].Value<int>("code") == 404)
            {
                // Object does not exist
                Console.WriteLine($"Object {issuerID}.{objectSuffix} not found!");
                return $"{issuerID}.{objectSuffix}";
            }
            else
            {
                // Something else went wrong...
                Console.WriteLine(jsonResponse.ToString());
                return $"{issuerID}.{objectSuffix}";
            }
        }

        // Object exists
        GenericObject existingObject = JsonConvert.DeserializeObject<GenericObject>(jsonResponse.ToString());

        GenericObject patchBody = new GenericObject();

        if(existingObject.TextModulesData != null)
        {
            patchBody.TextModulesData = existingObject.TextModulesData;
            foreach (var item in patchBody.TextModulesData)
            {
                if (item.Id.Equals("checks"))
                {
                    item.Body = checks;
                }
                if (item.Id.Equals("totalchecks"))
                {
                    item.Body = totalchecks;
                }
            }
        }

        responseStream = service.Genericobject
            .Patch(patchBody, $"{issuerID}.{objectSuffix}")
            .ExecuteAsStream();

        responseReader = new StreamReader(responseStream);
        jsonResponse = JObject.Parse(responseReader.ReadToEnd());

        Console.WriteLine("Object patch response");
        Console.WriteLine(jsonResponse.ToString());

        return $"{issuerID}.{objectSuffix}";
    }

    public string CreateJWTExistingObjects(string classSuffix, string objectSuffix)
    {
        // Ignore null values when serializing to/from JSON
        JsonSerializerSettings excludeNulls = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        // Multiple pass types can be added at the same time
        // At least one type must be specified in the JWT claims
        // Note: Make sure to replace the placeholder class and object suffixes
        Dictionary<string, Object> objectsToAdd = new Dictionary<string, Object>();

        // Generic passes
        objectsToAdd.Add("genericObjects", new List<GenericObject>
    {
      new GenericObject
      {
        Id = $"{issuerID}.{objectSuffix}",
        ClassId = $"{issuerID}.{classSuffix}"
      }
    });

        // Create a JSON representation of the payload
        JObject serializedPayload = JObject.Parse(
            JsonConvert.SerializeObject(objectsToAdd, excludeNulls));

        // Create the JWT as a JSON object
        JObject jwtPayload = JObject.Parse(JsonConvert.SerializeObject(new
        {
            iss = credentials.Id,
            aud = "google",
            origins = new string[]
          {
        HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority)
          },
            typ = "savetowallet",
            payload = serializedPayload
        }));

        // Deserialize into a JwtPayload
        JwtPayload claims = JwtPayload.Deserialize(jwtPayload.ToString());

        // The service account credentials are used to sign the JWT
        RsaSecurityKey key = new RsaSecurityKey(credentials.Key);
        SigningCredentials signingCredentials = new SigningCredentials(
            key, SecurityAlgorithms.RsaSha256);
        JwtSecurityToken jwt = new JwtSecurityToken(
            new JwtHeader(signingCredentials), claims);
        string token = new JwtSecurityTokenHandler().WriteToken(jwt);

        Console.WriteLine("Add to Google Wallet link");
        Console.WriteLine($"https://pay.google.com/gp/v/save/{token}");

        return $"https://pay.google.com/gp/v/save/{token}";
    }
}