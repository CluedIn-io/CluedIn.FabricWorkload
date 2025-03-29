// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Azure.Core;
using Azure.Identity;
using Azure.Storage.Files.DataLake;

using Boilerplate.Contracts;
using Boilerplate.Items;
using Boilerplate.Services;

using CsvHelper;
using CsvHelper.Configuration;

using Fabric_Extension_BE_Boilerplate;
using Fabric_Extension_BE_Boilerplate.Services.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Fabric.Api;

using Serilog;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Boilerplate.Controllers;

internal record IngestionResult(Guid CleanProjectId, string MappingJson);
internal partial class IngestionEndpointOperation
{
    public IngestionEndpointOperation(
        ILogger<IngestionEndpointOperation> logger,
        IHttpClientFactory httpClientFactory,
        CleanProjectItem item,
        IAuthenticationService authenticationService,
        AuthorizationContext authorizationContext)
    {
        Logger = logger;
        HttpClientFactory = httpClientFactory;
        Item = item;
        AuthenticationService = authenticationService;
        AuthorizationContext = authorizationContext;
    }
    protected const string ApplicationJsonContentType = "application/json";
    private static readonly TimeSpan DelayBeforeOperation = TimeSpan.FromSeconds(1);
    private const int MaximumVocabularyCreationPoll = 10;
    private static readonly TimeSpan DelayAfterVocabularyCreationPoll = TimeSpan.FromSeconds(1);
    private static readonly JsonSerializerOptions HeaderSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
    };
    public bool UseGetEntityTypeInfoWithTemplate { get; set; } = true;

    public async Task<IngestionResult> Setup(CancellationToken cancellationToken)
    {
        await InitializeAsync(cancellationToken);
        await Task.Delay(DelayBeforeOperation);
        await LoginAsync(cancellationToken);
        await Task.Delay(DelayBeforeOperation);
        await CreateDataSourceSetAsync(FileSource, cancellationToken);
        await Task.Delay(DelayBeforeOperation);
        await CreateDataSourceAsync(FileSource, cancellationToken);
        await Task.Delay(DelayBeforeOperation);
        await CreateDataSetAsync(FileSource, cancellationToken);
        await Task.Delay(DelayBeforeOperation);
        await UpdateDataSourceLogoAsync(FileSource, cancellationToken);
        await Task.Delay(DelayBeforeOperation);
        await CreateEntityTypeIfNotExistsAsync(FileSource, cancellationToken);
        await Task.Delay(DelayBeforeOperation);
        await CreateVocabularyIfNotExistsAsync(FileSource, cancellationToken);
        await Task.Delay(DelayBeforeOperation);
        var headerColumns = await StreamToIngestionEndpointAsync(FileSource, cancellationToken);
        await Task.Delay(DelayBeforeOperation);
        await CreateAutoAnnotationAsync(FileSource, cancellationToken);
        await Task.Delay(DelayBeforeOperation);
        await GetAnnotationIdAsync(FileSource, cancellationToken);
        await Task.Delay(DelayBeforeOperation);
        await SetOriginEntityCodeKeyAsync(FileSource, "--auto-generated--", cancellationToken);
        await Task.Delay(DelayBeforeOperation);
        await GetVocabularyKeyToAnnotationKeyMappingAsync(FileSource, cancellationToken);
        await Task.Delay(DelayBeforeOperation);
        await GetColumnToVocabularyKeyMappingAsync(FileSource, cancellationToken);
        await Task.Delay(DelayBeforeOperation);
        await CommitDataSetAsync(FileSource, cancellationToken);
        await Task.Delay(DelayBeforeOperation);
        var cleanProjectId = await CreateCleanProjectAsync(FileSource, cancellationToken);

        var mapping = headerColumns.Select((column, index) => new
        {
            Column = column,
            Mapped = FileSource.ColumnToVocabularyKeyMapping[index]
        }).ToDictionary(x => x.Column, x => x.Mapped.Value);
        return new (cleanProjectId, JsonSerializer.Serialize(mapping));
    }

    private async Task UpdateDataSourceLogoAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        try
        {
            var fileSourceType = typeof(FileSource);
            var assembly = fileSourceType.Assembly;
            var resourceName = $"{fileSourceType.Namespace}.Icon.fabricLogo.svg";

            await using var stream = assembly.GetManifestResourceStream(resourceName);

            var serverUris = await GetServerUris(cancellationToken).ConfigureAwait(false);

            var requestUri = new Uri(serverUris.UploadApiUri, $"api/datasource/{fileSource?.DataSourceId}/logo");
            var client = HttpClientFactory.CreateClient(IngestionConstants.AllowUntrustedSSLClient);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.Add("Content-Type", "image/svg+xml");
            content.Add(streamContent, "ds-logo", "blob");
            requestMessage.Headers.Add("clientid", Organization.ClientId);
            requestMessage.Content = content;

            var response = await SendRequestAsync(requestMessage, cancellationToken, true).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update logo for datasource {DatasourceId}.", fileSource?.DataSourceId);
        }
    }

    private FileSource FileSource { get; set; }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        Organization = new Organization
        {
            ClientId = Item.OrganizationName,
            EmailDomain = Item.Domain,
            Password = Item.UserPassword,
            UserName = Item.UserEmail,
        };

        var entityType = $"Fabric_{Item.ItemObjectId:N}";
        FileSource = new FileSource
        {
            UploadFilePath = Item.InputFilePath,
            VocabularyName = entityType,
            EntityType = entityType,
            IsExternalUploadFilePath = true,
        };

        Options = new IngestionOptions()
        {
           IngestionBatchSize = 200,
           IngestionRequestsDelayInMilliseconds = 200,
           UseIngestionEndpoint = true,
           MaxCleanSize = 1000,
        };
    }

    internal async Task<bool> GetCleaningProjectExistsAsync(Guid cleanProjectId, CancellationToken cancellationToken)
    {
        await InitializeAsync(cancellationToken);
        await LoginAsync(cancellationToken);
        var serverUris = await GetServerUris(cancellationToken).ConfigureAwait(false);
        var requestUri = serverUris.UiGraphqlUri;
        var client = HttpClientFactory.CreateClient(IngestionConstants.AllowUntrustedSSLClient);

        var replacedBody = RequestTemplates.GetCleanProjectDetailAsync(cleanProjectId);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(replacedBody, Encoding.UTF8, ApplicationJsonContentType),
        };

        var response = await SendRequestAsync(requestMessage, cancellationToken, true).ConfigureAwait(false);

        var result = await response.Content
            .DeserializeToAnonymousTypeAsync(new
            {
                data = new
                {
                    preparation = new
                    {
                        cleanProjectDetail = new
                        {
                            id = (Guid?)null,
                        }
                    },
                },
            })
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid result because it is empty.");

        var responseCleanProjectId = result.data?.preparation?.cleanProjectDetail?.id;
        return responseCleanProjectId == cleanProjectId;
    }

    private async Task<Guid> CreateCleanProjectAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        var serverUris = await GetServerUris(cancellationToken).ConfigureAwait(false);
        var requestUri = serverUris.UiGraphqlUri;
        var client = HttpClientFactory.CreateClient(IngestionConstants.AllowUntrustedSSLClient);

        var replacedBody = RequestTemplates.CreateCleanProjectAsync(fileSource.ConnectorConfigurationId, $"FabricCleanse_{Item.DisplayName}_{Item.ItemObjectId}", FileSource.VocabularyKeyToAnnotationKeyMapping);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(replacedBody, Encoding.UTF8, ApplicationJsonContentType),
        };

        var response = await SendRequestAsync(requestMessage, cancellationToken, true).ConfigureAwait(false);

        var result = await response.Content
            .DeserializeToAnonymousTypeAsync(new
            {
                data = new
                {
                    preparation = new
                    {
                        createNewCleanProject = new
                        {
                            id = (Guid?) null,
                        }
                    },
                },
            })
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid result because it is empty.");

        var cleanProjectId = result.data?.preparation?.createNewCleanProject?.id ?? throw new InvalidOperationException("Clean project id cannot be found.");
        return cleanProjectId;
    }

    private async Task CommitDataSetAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        var serverUris = await GetServerUris(cancellationToken).ConfigureAwait(false);
        var requestUri = serverUris.UiGraphqlUri;
        var client = HttpClientFactory.CreateClient(IngestionConstants.AllowUntrustedSSLClient);

        var replacedBody = RequestTemplates.CommitDataSetAsync(fileSource.DataSetId);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(replacedBody, Encoding.UTF8, ApplicationJsonContentType),
        };

        var response = await SendRequestAsync(requestMessage, cancellationToken, true).ConfigureAwait(false);
    }
    protected async Task CreateVocabularyIfNotExistsAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        var vocabularyId = await CreateVocabularyIfNotExistsAsync(fileSource.EntityType, fileSource.VocabularyName, cancellationToken);
        fileSource.VocabularyId = vocabularyId;
    }
    protected async Task CreateAutoAnnotationAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        var body = RequestTemplates.CreateAutoAnnotationAsync(
            dataSetId: fileSource.DataSetId,
            entityType: fileSource.EntityType,
            vocabularyName: fileSource.VocabularyName,
            vocabularyId: fileSource.VocabularyId);
        var response = await SendGraphQlRequestAsync(body, cancellationToken, requireAuthorization: true).ConfigureAwait(false);

        _ = await CheckResponse(response).ConfigureAwait(false);
        _ = await PollForVocabularyCreationCompletionAsync(fileSource.VocabularyName, cancellationToken).ConfigureAwait(false);
    }
    protected async Task<Guid> PollForVocabularyCreationCompletionAsync(string vocabularyName, CancellationToken cancellationToken)
    {
        // Server has issues if we create multiple vocabularies in quick succession,
        // Sometimes it says vocabulary does not exist, when it does,
        // We need to ensure that it exists first before processing

        for (var i = 0; i < MaximumVocabularyCreationPoll; ++i)
        {
            Logger.LogInformation("Waiting for {DelayAfterVocabularyCreation} before checking whether vocabulary {VocabularyName} exists.",
                DelayAfterVocabularyCreationPoll,
                vocabularyName);
            await Task.Delay(DelayAfterVocabularyCreationPoll, cancellationToken).ConfigureAwait(false);
            try
            {
                var vocabularyId = await GetVocabularyIdFromName(vocabularyName, cancellationToken).ConfigureAwait(false);

                if (vocabularyId != null)
                {
                    Logger.LogInformation("Finish polling for {VocabularyName}.", vocabularyName);
                    return vocabularyId.Value;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to poll for {VocabularyName}.", vocabularyName);
            }
        }

        throw new InvalidOperationException($"Failed to ensure that vocabulary {vocabularyName} exists.");
    }
    protected async Task<Guid> CreateVocabularyIfNotExistsAsync(string vocabularyName, string entityType, CancellationToken cancellationToken)
    {
        var vocabularyId = await GetVocabularyIdFromName(vocabularyName, cancellationToken).ConfigureAwait(false);

        if (vocabularyId != null)
        {
            Logger.LogInformation("Skipping creation of VocabularyName {VocabularyName} because it exists.", vocabularyName);
            return vocabularyId.Value;
        }

        Logger.LogInformation("Creating VocabularyName {VocabularyName} because it does not exist.", vocabularyName);
        return await CreateVocabularyAsync(vocabularyName, entityType, cancellationToken).ConfigureAwait(false);
    }

    protected async Task<Guid?> GetVocabularyIdFromName(string vocabularyName, CancellationToken cancellationToken)
    {
        var body = RequestTemplates.GetAllVocabulariesAsync(vocabularyName);
        var response = await SendGraphQlRequestAsync(body, cancellationToken, requireAuthorization: true).ConfigureAwait(false);

        var result = await response.Content
            .DeserializeToAnonymousTypeAsync(new
            {
                data = new
                {
                    management = new
                    {
                        vocabularies = new
                        {
                            data = new[]
                            {
                                new
                                {
                                    vocabularyId = (Guid?)null,
                                    vocabularyName = (string?)null,
                                    keyPrefix = (string?)null,
                                }
                            }
                        }
                    },
                },
            })
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid result because it is empty.");

        return result.data?.management?.vocabularies?.data?.SingleOrDefault(vocabulary => vocabulary.vocabularyName == vocabularyName)?.vocabularyId;
    }

    protected async Task<Guid> CreateVocabularyAsync(string vocabularyName, string entityType, CancellationToken cancellationToken)
    {
        var body = RequestTemplates.CreateVocabularyAsync(
            vocabularyName: vocabularyName,
            entityType: entityType);
        var response = await SendGraphQlRequestAsync(body, cancellationToken, requireAuthorization: true).ConfigureAwait(false);

        var result = await response.Content
            .DeserializeToAnonymousTypeAsync(new
            {
                data = new
                {
                    management = new
                    {
                        createVocabulary = new
                        {
                            vocabularyId = (Guid?)null,
                        }
                    },
                },
            })
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid result because it is empty.");

        return await PollForVocabularyCreationCompletionAsync(vocabularyName, cancellationToken).ConfigureAwait(false);
    }
    protected async Task CreateEntityTypeIfNotExistsAsync(string entityType, CancellationToken cancellationToken)
    {
        var entityId = await GetEntityTypeInfoAsync(entityType, cancellationToken).ConfigureAwait(false);

        if (entityId != null)
        {
            Logger.LogInformation("Skipping creation of Entity Type {EntityType} because it exists.", entityType);
            return;
        }

        Logger.LogInformation("Creating Entity Type {EntityType} because it does not exist.", entityType);
        await CreateEntityTypeAsync(entityType, cancellationToken).ConfigureAwait(false);
    }


    protected async Task<Guid?> GetEntityTypeInfoAsync(string entityType, CancellationToken cancellationToken)
    {
        var (json, errors) = await GetEntityTypeInfoAsync(entityType, UseGetEntityTypeInfoWithTemplate, cancellationToken).ConfigureAwait(false);

        if (UseGetEntityTypeInfoWithTemplate && errors.Any() && errors.Any(error => error.Message != null && error.Message.Contains("Unknown argument")))
        {
            Logger.LogInformation("Server does not support pagetemplate, falling back to no page template.");
            UseGetEntityTypeInfoWithTemplate = false;
            (json, errors) = await GetEntityTypeInfoAsync(entityType, UseGetEntityTypeInfoWithTemplate, cancellationToken).ConfigureAwait(false);
        }

        CheckForErrors(errors);

        var result = json
            .DeserializeToAnonymousType(new
            {
                data = new
                {
                    management = new
                    {
                        getEntityTypeInfo = new
                        {
                            id = (string?)null,
                            type = (string?)null,
                            route = (string?)null,
                            icon = (string?)null,
                        }
                    },
                },
            }) ?? throw new InvalidOperationException("Invalid result because it is empty.");

        var id = result.data?.management?.getEntityTypeInfo?.id;
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        if (Guid.TryParse(id, out var parsedId))
        {
            return parsedId;
        }

        throw new InvalidOperationException($"The id value '{id}' returned is not a guid.");
    }

    private async Task<(string json, IEnumerable<GraphQLError> Errors)> GetEntityTypeInfoAsync(string entityType, bool withPageTemplate, CancellationToken cancellationToken)
    {
        var body = RequestTemplates.GetEntityTypeInfoAsync(entityType, withPageTemplate);
        var response = await SendGraphQlRequestAsync(body, cancellationToken, requireAuthorization: true, throwIfNotSuccessCode: false).ConfigureAwait(false);

        return await GetJsonResponse(response, GetSerializerOptions());
    }
    private static JsonSerializerOptions GetSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
    }

    protected Task CreateEntityTypeIfNotExistsAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        return CreateEntityTypeIfNotExistsAsync(fileSource.EntityType, cancellationToken);
    }

    protected async Task CreateEntityTypeAsync(string entityType, CancellationToken cancellationToken)
    {
        var serverUris = await GetServerUris(cancellationToken).ConfigureAwait(false);
        var requestUri = serverUris.UiGraphqlUri;

        var replacedBody = RequestTemplates.CreateEntityTypeAsync(
            entityType: entityType,
            entityTypeRoute: entityType.ToLower());

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(replacedBody, Encoding.UTF8, ApplicationJsonContentType),
        };
        var response = await SendRequestAsync(requestMessage, cancellationToken, true).ConfigureAwait(false);

        var result = await response.Content
            .DeserializeToAnonymousTypeAsync(new
            {
                data = new
                {
                    management = new
                    {
                        createEntityTypeConfigurationV2 = new
                        {
                            type = (string?)null,
                            route = (string?)null,
                            icon = (string?)null,
                        }
                    },
                },
            })
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid result because it is empty.");

        var type = result.data?.management?.createEntityTypeConfigurationV2?.type ?? throw new InvalidOperationException("Entity type not found in result.");
    }
    protected async Task<HttpResponseMessage> GetDataSourceByIdAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        var body = RequestTemplates.GetDataSourceByIdAsync(fileSource.DataSourceId);
        var response = await SendGraphQlRequestAsync(body, cancellationToken, requireAuthorization: true).ConfigureAwait(false);
        return response;
    }
    protected async Task GetAnnotationIdAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        var response = await GetDataSourceByIdAsync(fileSource, cancellationToken).ConfigureAwait(false);
        var result = await response.Content
            .DeserializeToAnonymousTypeAsync(new
            {
                data = new
                {
                    inbound = new
                    {
                        dataSource = new
                        {
                            dataSets = new[]
                            {
                                new
                                {
                                    annotationId = (int?)null,
                                },
                            },
                            connectorConfigurationId = (Guid?)null,
                        },
                    },
                },
            }).ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid result because it is empty.");

        int? annotationId = result.data?.inbound?.dataSource?.dataSets?[0]?.annotationId ?? throw new InvalidOperationException("AnnotationId is not found in result.");
        Guid? connectorConfigurationId = result.data?.inbound?.dataSource?.connectorConfigurationId ?? throw new InvalidOperationException("AnnotationId is not found in result.");

        fileSource.AnnotationId = annotationId.Value;
        fileSource.ConnectorConfigurationId = connectorConfigurationId.Value;
    }
    protected async Task SetOriginEntityCodeKeyAsync(FileSource fileSource, string originEntityCodeKey, CancellationToken cancellationToken)
    {
        var body = RequestTemplates.SetOriginEntityCodeKeyAsync(
            annotationId: fileSource.AnnotationId,
            originEntityCodeKey: originEntityCodeKey);
        var response = await SendGraphQlRequestAsync(body, cancellationToken, requireAuthorization: true).ConfigureAwait(false);
        _ = await CheckResponse(response);
    }
    private static async Task<(string Json, IEnumerable<GraphQLError> Errors)> GetJsonResponse(HttpResponseMessage response, JsonSerializerOptions? options = null)
    {
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var result = json
                    .DeserializeToAnonymousType(new
                    {
                        errors = (GraphQLError[]?)null,
                    }, options) ?? throw new InvalidOperationException("Invalid result because it is empty.");

        return (json, result.errors ?? Array.Empty<GraphQLError>());
    }
    private static async Task<string> CheckResponse(HttpResponseMessage response)
    {
        var (json, errors) = await GetJsonResponse(response);
        CheckForErrors(errors);
        return json;
    }

    private static void CheckForErrors(IEnumerable<GraphQLError> errors)
    {
        if (errors != null && errors.Any())
        {
            throw new InvalidOperationException($"Failed to perform operation because '{string.Join(',', errors.Select(error => error.Message))}'.");
        }
    }

    protected async Task GetColumnToVocabularyKeyMappingAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        var body = RequestTemplates.GetDataSetByIdAsync(dataSetId: fileSource.DataSetId);
        var response = await SendGraphQlRequestAsync(body, cancellationToken, requireAuthorization: true).ConfigureAwait(false);

        var result = await response.Content
            .DeserializeToAnonymousTypeAsync(new
            {
                data = new
                {
                    inbound = new
                    {
                        dataSet = new
                        {
                            originalFields = (string[])null,
                            fieldMappings = new[]
                            {
                                new
                                {
                                    originalField = (string?)null,
                                    key = (string?)null,
                                },
                            },
                        },
                    },
                },
            }).ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid result because it is empty.");

        var originalFields = result.data?.inbound?.dataSet?.originalFields
                                   ?? throw new InvalidOperationException("Invalid result because originalFields is not found.");
        var fieldMappings = result.data?.inbound?.dataSet?.fieldMappings
                                   ?? throw new InvalidOperationException("Invalid result because fieldMappings is not found.");

        if (fieldMappings.Any(prop =>
                string.IsNullOrWhiteSpace(prop.key) || string.IsNullOrWhiteSpace(prop.originalField)))
        {
            throw new InvalidOperationException("Invalid result because key or originalField is null/whitespace.");
        }
        if (originalFields.Any(originalField => string.IsNullOrWhiteSpace(originalField)))
        {
            throw new InvalidOperationException("Invalid result because originalField is null/whitespace.");
        }

        var mapping = fieldMappings.ToDictionary(item => item.originalField, item => item.key);
        fileSource.ColumnToVocabularyKeyMapping = originalFields.Select(originalField =>
        {
            return new KeyValuePair<string, string>(originalField, mapping[originalField]);
        }).ToList();
    }

    protected async Task GetVocabularyKeyToAnnotationKeyMappingAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        var body = RequestTemplates.GetAnnotationById(annotationId: fileSource.AnnotationId);
        var response = await SendGraphQlRequestAsync(body, cancellationToken, requireAuthorization: true).ConfigureAwait(false);

        var result = await response.Content
            .DeserializeToAnonymousTypeAsync(new
            {
                data = new
                {
                    preparation = new
                    {
                        annotation = new
                        {
                            annotationProperties = new[]
                            {
                                new
                                {
                                    key = (string?)null,
                                    useAsEntityCode = (bool?)null,
                                    useSourceCode = (bool?)null,
                                    vocabKey = (string?)null,
                                },
                            },
                        },
                    },
                },
            }).ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid result because it is empty.");

        var annotationProperties = result.data?.preparation?.annotation?.annotationProperties
                                   ?? throw new InvalidOperationException("Invalid result because properties is not found.");

        if (annotationProperties.Any(prop =>
                string.IsNullOrWhiteSpace(prop.key) || string.IsNullOrWhiteSpace(prop.vocabKey)))
        {
            throw new InvalidOperationException("Invalid result because key or vocabKey is null/whitespace.");
        }
        var properties = annotationProperties.ToDictionary(prop => prop.vocabKey!, prop => prop.key!);

        fileSource.VocabularyKeyToAnnotationKeyMapping = properties;
    }
    private async Task CreateDataSourceAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        var body = RequestTemplates.CreateDataSourceAsync(
            dataSourceSetId: fileSource.DataSourceSetId,
            dataSourceName: $"FabricIngest_{Item.DisplayName}_{Item.ItemObjectId:N}",
            userId: Organization.UserId);
        var response = await SendGraphQlRequestAsync(body, cancellationToken, requireAuthorization: true).ConfigureAwait(false);

        var result = await response.Content
            .DeserializeToAnonymousTypeAsync(new
            {
                data = new
                {
                    inbound = new
                    {
                        createDataSource = new
                        {
                            id = (int?)null
                        },
                    },
                },
            })
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid result because it is empty.");

        int? resultDataSourceId = result.data?.inbound?.createDataSource?.id ?? throw new InvalidOperationException("DataSourceSet is not found in result.");

        fileSource.DataSourceId = resultDataSourceId.Value;
    }

    protected async Task CreateDataSourceSetAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        var body = RequestTemplates.CreateDataSourceSetAsync(
            dataSourceSetName: $"Fabric_{Item.DisplayName}_{Item.ItemObjectId:N}",//Path.GetFileNameWithoutExtension(fileSource.UploadFilePath),
            userId: Organization.UserId);
        var response = await SendGraphQlRequestAsync(body, cancellationToken, requireAuthorization: true).ConfigureAwait(false);

        var result = await response.Content
            .DeserializeToAnonymousTypeAsync(new
            {
                data = new
                {
                    inbound = new
                    {
                        createDataSourceSet = (int?)null,
                    },
                },
            })
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid result because it is empty.");

        int? resultDataSourceSetId = result.data?.inbound?.createDataSourceSet ?? throw new InvalidOperationException("DataSourceSetId is not found in result.");

        fileSource.DataSourceSetId = resultDataSourceSetId.Value;
    }

    //private async Task CreateLoginToken()
    //{
    //    var client = new HttpClient();
    //    var request = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1.nip.io:9001/connect/token");
    //    request.Headers.Add("Pragma", "no-cache");
    //    request.Headers.Add("Cache-Control", "no-cache");
    //    request.Headers.Add("Accept-Language", "en-US,en;q=0.9");
    //    var collection = new List<KeyValuePair<string, string>>
    //    {
    //        new("username", "admin@foobar.com"),
    //        new("password", "Foobar23!"),
    //        new("grant_type", "password"),
    //        new("client_id", "foobar")
    //    };
    //    var content = new FormUrlEncodedContent(collection);
    //    request.Content = content;
    //    var response = await client.SendAsync(request);
    //    response.EnsureSuccessStatusCode();
    //    Console.WriteLine(await response.Content.ReadAsStringAsync());

    //}
    private async Task<string[]> StreamToIngestionEndpointAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(fileSource.UploadFilePath);
        Logger.LogInformation("Begin streaming {FileName} to ingestion endpoint.", fileName);

        var batchSize = Options.IngestionBatchSize;
        await using var fileStream = await GetUploadFileStream(fileSource);
        using var streamReader = new StreamReader(fileStream);
        using var csv = new CsvReader(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            BadDataFound = null,
        });
        csv.Context.RegisterClassMap<MyClassWithDictionaryMapper>();

        var batch = new List<Dictionary<string, string>>(batchSize);

        var totalSent = 0;
        try
        {
            await foreach (var currentRecord in csv.GetRecordsAsync<CsvRow>())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.LogInformation("Aborting streaming because cancellation is requested.");
                    return [];
                }
                batch.Add(currentRecord.Columns);
                if (batch.Count == batchSize)
                {
                    var success = await SendBatchToIngestionEndpointAsync(fileSource, batch, cancellationToken).ConfigureAwait(false);
                    if (success)
                    {
                        totalSent += batchSize;
                        Logger.LogDebug("Total rows sent {TotalSent}.", totalSent);
                    }
                    else
                    {
                        Logger.LogWarning("Failed to send batch. Ignoring batch.");
                    }
                    batch.Clear();
                    if (totalSent >= Options.MaxCleanSize)
                    {
                        Logger.LogInformation("Max count reached {Count}.", totalSent);
                        break;
                    }
                    if (Options.IngestionRequestsDelayInMilliseconds > 0)
                    {
                        await Task.Delay(Options.IngestionRequestsDelayInMilliseconds, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send data.");
        }

        if (batch.Count > 0)
        {
            Logger.LogInformation("Sending last batch of rows.");
            var success = await SendBatchToIngestionEndpointAsync(fileSource, batch, cancellationToken).ConfigureAwait(false);
            if (success)
            {
                totalSent += batch.Count;
                Logger.LogDebug("Total rows sent {TotalSent}.", totalSent);
            }
            batch.Clear();
        }
        Logger.LogInformation("Finished streaming {FileName} to ingestion endpoint. Total Rows sent {TotalSent}", fileName, totalSent);
        return csv.HeaderRecord;
    }

    private async Task<Stream> GetUploadFileStream(FileSource fileSource)
    {
        var credential = await AuthenticationService.GetTokenCredential(AuthorizationContext);

        var accountName = "onelake";
        var dfsUri = $"https://{accountName}.dfs.fabric.microsoft.com";

        var fabricClient = new FabricClient(credential);
        var workspace = await fabricClient.Core.Workspaces.GetWorkspaceAsync(Item.InputFileLakehouse.WorkspaceId);
        var lakehouse = await fabricClient.Lakehouse.Items.GetLakehouseAsync(Item.InputFileLakehouse.WorkspaceId, Item.InputFileLakehouse.Id);
        var dataLakeServiceClient = new DataLakeServiceClient(
            new Uri(dfsUri),
            credential);
        var fileSystemClient = dataLakeServiceClient.GetFileSystemClient(workspace.Value.DisplayName);
        var directoryClient = fileSystemClient.GetDirectoryClient($"{lakehouse.Value.DisplayName}.Lakehouse");
        var fileName = Path.GetFileName(Item.InputFilePath);
        var directory = Item.InputFilePath[..^(fileName.Length + 1)];
        directoryClient = directoryClient.GetSubDirectoryClient(directory);
        var fileClient = directoryClient.GetFileClient(Path.GetFileName(fileSource.UploadFilePath));
        return await fileClient.OpenReadAsync();
    }

    private async Task<bool> SendBatchToIngestionEndpointAsync(FileSource fileSource, List<Dictionary<string, string>> batch, CancellationToken cancellationToken)
    {
        var totalRetries = 10;
        var delayBetweenRetries = TimeSpan.FromSeconds(2);
        for (var i = 0; i < totalRetries; ++i)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogInformation("Aborting streaming of batch because cancellation is requested.");
                return false;
            }

            try
            {
                var serverUris = await GetServerUris(cancellationToken).ConfigureAwait(false);
                var requestUri = new Uri(serverUris.UploadApiUri, $"api/endpoint/{fileSource.DataSetId}");

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = new StringContent(JsonSerializer.Serialize(batch), Encoding.UTF8, ApplicationJsonContentType),
                };
                var response = await SendRequestAsync(requestMessage, cancellationToken, requireAuthorization: true, suppressDebug: true).ConfigureAwait(false);
                var result = await response.Content
                    .DeserializeToAnonymousTypeAsync(new
                    {
                        success = (bool?)null,
                        received = (int?)null,
                        warning = (bool?)null,
                        error = (bool?)null,
                    })
                    .ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid result because it is empty.");

                if (result.success.GetValueOrDefault())
                {
                    Logger.LogDebug("Successfully streamed batch of size {BatchSize}.", batch.Count);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error trying to send to ingestion endpoint. retrying in a while.");
                await Task.Delay(delayBetweenRetries, cancellationToken).ConfigureAwait(false);
            }
        }

        return false;
    }
    protected async Task LoginAsync(CancellationToken cancellationToken)
    {
        var serverUris = await GetServerUris(cancellationToken).ConfigureAwait(false);
        var requestUri = new Uri(serverUris.AuthApiUri, "connect/token");

        var client = HttpClientFactory.CreateClient(IngestionConstants.AllowUntrustedSSLClient);

        var requestBody = new Dictionary<string, string>
        {
            ["username"] = Organization.UserName,
            ["password"] = Organization.Password,
            ["grant_type"] = "password",
            ["client_id"] = Organization.ClientId,
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new FormUrlEncodedContent(requestBody),
        };

        var response = await SendRequestAsync(requestMessage, cancellationToken, true).ConfigureAwait(false);

        var result = await response.Content
            .DeserializeToAnonymousTypeAsync(new { access_token = "" })
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid result because it is empty.");

        Organization.AccessToken = result.access_token;

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(Organization.AccessToken);
        var userIdClaimValue = token.Payload.Claims.FirstOrDefault(claim => claim.Type == "Id")?.Value ?? throw new InvalidOperationException("Id claim is not found in access token.");
        var organizationIdClaimValue = token.Payload.Claims.FirstOrDefault(claim => claim.Type == "OrganizationId")?.Value ?? throw new InvalidOperationException("OrganizationId claim is not found in access token.");

        if (!Guid.TryParse(userIdClaimValue, out var parsedUserId))
        {
            throw new InvalidOperationException($"Id claim value '{userIdClaimValue}' is not a valid guid.");
        }

        if (!Guid.TryParse(organizationIdClaimValue, out var parsedOrganizationId))
        {
            throw new InvalidOperationException($"OrganizationId claim value '{organizationIdClaimValue}' is not a valid guid.");
        }

        Organization.UserId = parsedUserId;
        Organization.OrganizationId = parsedOrganizationId;
    }

    protected virtual Task<ServerUriCollection> GetServerUris(CancellationToken cancellationToken)
    {
        var serverUrlWithTrailingSlash = EnsureTrailingSlash(new Uri($"https://app.{Item.Domain}/"));
        return Task.FromResult(new ServerUriCollection(
            AuthApiUri: new Uri(serverUrlWithTrailingSlash, "auth/"),
            PublicApiUri: new Uri(serverUrlWithTrailingSlash, "public/"),
            WebApiUri: new Uri(serverUrlWithTrailingSlash, "api/"),
            UiGraphqlUri: new Uri(serverUrlWithTrailingSlash, "graphql/"),
            UploadApiUri: new Uri(serverUrlWithTrailingSlash, "upload/")
            ));
    }
    private static Uri EnsureTrailingSlash(Uri uri)
    {
        if (!uri.AbsoluteUri.EndsWith("/"))
        {
            return new Uri(uri.AbsoluteUri + "/");
        }

        return uri;
    }

    private void AddAuthorizationHeader(HttpRequestMessage requestMessage)
    {
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Organization.AccessToken);
    }

    protected async Task<HttpResponseMessage> SendGraphQlRequestAsync(
        string body,
        CancellationToken cancellationToken,
        bool requireAuthorization = false,
        Action<HttpClient>? configureClient = null,
        bool suppressDebug = false,
        bool throwIfNotSuccessCode = true)
    {
        var serverUris = await GetServerUris(cancellationToken).ConfigureAwait(false);
        var requestUri = serverUris.UiGraphqlUri;
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(body, Encoding.UTF8, ApplicationJsonContentType),
        };

        return await SendRequestAsync(
            requestMessage,
            cancellationToken,
            requireAuthorization: requireAuthorization,
            configureClient: configureClient,
            suppressDebug: suppressDebug,
            throwIfNotSuccessCode: throwIfNotSuccessCode)
            .ConfigureAwait(false);
    }

    protected async Task<HttpResponseMessage> SendRequestAsync(
        HttpRequestMessage requestMessage,
        CancellationToken cancellationToken,
        bool requireAuthorization = false,
        Action<HttpClient>? configureClient = null,
        bool suppressDebug = false,
        bool throwIfNotSuccessCode = true)
    {
        var client = HttpClientFactory.CreateClient(IngestionConstants.AllowUntrustedSSLClient);

        configureClient?.Invoke(client);

        if (requireAuthorization)
        {
            AddAuthorizationHeader(requestMessage);
        }

        if (!suppressDebug && (requestMessage.Content is StringContent || requestMessage.Content is FormUrlEncodedContent))
        {
            var requestContent = await requestMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var headersWithRedactedAuthorization = GetHeaderWithRedactedAuthorization(requestMessage);
            Logger.LogDebug("""
                    Making request to {Uri}
                    {Headers}
                    {Content}
                    """,
                        requestMessage.RequestUri,
                        SerializeHeaders(headersWithRedactedAuthorization),
                        requestContent);
        }
        else
        {
            Logger.LogDebug("Making request to {Uri}.", requestMessage.RequestUri);
        }

        var response = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!suppressDebug)
        {
            Logger.LogDebug("""
                    Got response from request to {Uri}
                    {Headers}
                    {Content}
                    """,
                        requestMessage.RequestUri,
                        SerializeHeaders(response.Headers),
                        content);
        }

        if (!response.IsSuccessStatusCode && throwIfNotSuccessCode)
        {
            throw new InvalidOperationException("Failed to perform request successfully.");
        }
        return response;
    }

    private static string SerializeHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        var simplified = headers.ToDictionary(kvp => kvp.Key, kvp => string.Join(",", kvp.Value));
        return JsonSerializer.Serialize(simplified, HeaderSerializerOptions);
    }

    private Dictionary<string, IEnumerable<string>> GetHeaderWithRedactedAuthorization(HttpRequestMessage requestMessage)
    {
        var headersWithRedactedAuthorization = new Dictionary<string, IEnumerable<string>>();
        foreach (var kvp in requestMessage.Headers)
        {
            if (kvp.Key == "Authorization" && kvp.Value.Count() == 1 && kvp.Value.SingleOrDefault() == $"Bearer {Organization?.AccessToken}")
            {
                headersWithRedactedAuthorization.Add("Authorization", ["Bearer [Redacted]"]);
                continue;
            }
            headersWithRedactedAuthorization.Add(kvp.Key, kvp.Value);
        }

        return headersWithRedactedAuthorization;
    }
    private async Task CreateDataSetAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        var body = RequestTemplates.CreateDataSetAsync(
            dataSourceId: fileSource.DataSourceId,
            userId: Organization.UserId,
            entityType: fileSource.EntityType + "Dummy");
        var response = await SendGraphQlRequestAsync(body, cancellationToken, requireAuthorization: true).ConfigureAwait(false);

        var result = await response.Content
            .DeserializeToAnonymousTypeAsync(new
            {
                data = new
                {
                    inbound = new
                    {
                        createDataSets = new[]
                        {
                       new
                       {
                           id = (Guid?)null,
                       }
                        },
                    },
                },
            })
            .ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid result because it is empty.");

        var dataSetsArray = result.data?.inbound?.createDataSets;
        if (dataSetsArray == null || dataSetsArray.Length == 0 || dataSetsArray[0].id == null)
        {
            throw new InvalidOperationException("DataSourceSet is not found in result.");
        }

        if (dataSetsArray[0].id.GetValueOrDefault() == Guid.Empty)
        {
            throw new InvalidOperationException("DataSourceSet is empty guid.");
        }

        fileSource.DataSetId = dataSetsArray[0].id.GetValueOrDefault();
    }

    private async Task SendSampleDataAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        var sampleData = await GetSampleDataAsync(fileSource, cancellationToken).ConfigureAwait(false);
        await SendBatchToIngestionEndpointAsync(fileSource, sampleData, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<Dictionary<string, string>>> GetSampleDataAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        await using var fileStream = await GetUploadFileStream(fileSource);
        using (var streamReader = new StreamReader(fileStream))
        using (var csv = new CsvReader(streamReader, CultureInfo.InvariantCulture))
        {
            csv.Context.RegisterClassMap<MyClassWithDictionaryMapper>();
            await foreach (var record in csv.GetRecordsAsync<CsvRow>())
            {
                return new List<Dictionary<string, string>>
                {
                    record.Columns,
                };
            }
        }

        return new List<Dictionary<string, string>>();
    }

    private async Task ModifyDataSetAutoSubmitAsync(FileSource fileSource, CancellationToken cancellationToken)
    {
        var body = RequestTemplates.ModifyDataSetAutoSubmitAsync(fileSource.DataSetId);
        var response = await SendGraphQlRequestAsync(body, cancellationToken, requireAuthorization: true).ConfigureAwait(false);
    }
    public class MyClassWithDictionaryMapper : ClassMap<CsvRow>
    {
        public MyClassWithDictionaryMapper()
        {

            Map(m => m.Columns).Convert
               (row => row.Row.HeaderRecord.Select
                (column => new { column, value = row.Row.GetField(column) })
                .ToDictionary(d => d.column, d => d.value)
                );
        }
    }
    protected class GraphQLError
    {
        public string? Message { get; set; }
    }
    public class CsvRow
    {
        public Dictionary<string, string> Columns { get; set; }
    }

    protected Organization? Organization { get; set; }
    protected IngestionOptions? Options { get; set; }
    public IHttpClientFactory HttpClientFactory { get; }
    public CleanProjectItem Item { get; }
    public IAuthenticationService AuthenticationService { get; }
    public AuthorizationContext AuthorizationContext { get; }
    public ILogger<IngestionEndpointOperation> Logger { get; }
}
