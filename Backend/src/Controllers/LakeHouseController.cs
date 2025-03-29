// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Azure.Storage.Files.DataLake;

using Boilerplate.Constants;
using Boilerplate.Contracts;
using Boilerplate.Services;

using CsvHelper.Configuration;
using CsvHelper;

using Fabric_Extension_BE_Boilerplate.Services.Ingestion;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Fabric.Api;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

using static Boilerplate.Controllers.IngestionEndpointOperation;
using Fabric_Extension_BE_Boilerplate;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;

namespace Boilerplate.Controllers
{
    [ApiController]
    public class LakehouseController : ControllerBase
    {
        private const int MaxPreviewRows = 200;
        private static readonly IList<string> OneLakeScopes = new[] { $"{EnvironmentConstants.OneLakeResourceId}/.default" };
        private static readonly IList<string> ScopesForReadLakehouseFile = new[] { WorkloadScopes.FabricLakehouseReadAll, WorkloadScopes.FabricLakehouseReadWriteAll };
        private static readonly IList<string> ScopesForWriteLakehouseFile = new[] { WorkloadScopes.FabricLakehouseReadWriteAll };

        private readonly ILogger<LakehouseController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILakehouseClientService _lakeHouseClientService;

        public IHttpClientFactory HttpClientFactory { get; }

        public LakehouseController(
            ILogger<LakehouseController> logger,
            IHttpContextAccessor httpContextAccessor,
            IAuthenticationService authenticationService,
            ILakehouseClientService lakeHouseClientService,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _authenticationService = authenticationService;
            _lakeHouseClientService = lakeHouseClientService;
            HttpClientFactory = httpClientFactory;
        }

        [HttpGet("getLakehouseFile")]
        public async Task<IActionResult> GetLakehouseFile(string source)
        {
            var authorizationContext = await _authenticationService.AuthenticateDataPlaneCall(_httpContextAccessor.HttpContext, allowedScopes: ScopesForReadLakehouseFile);
            var lakeHouseAccessToken = await _authenticationService.GetAccessTokenOnBehalfOf(authorizationContext, OneLakeScopes);

            var data = await _lakeHouseClientService.GetLakehouseFile(lakeHouseAccessToken, source);

            if (string.IsNullOrEmpty(data))
            {
                _logger.LogWarning($"GetLakehouseFile returned empty data for source: {source}");
                // Return a 204 No Content status code for empty data
                return NoContent();
            }

            _logger.LogInformation($"GetLakehouseFile succeeded for source: {source}");
            return Ok(data);
        }

        [HttpPut("writeToLakehouseFile")]
        public async Task<IActionResult> WriteToLakehouseFile([FromBody] WriteToLakehouseFileRequest request)
        {
            var authorizationContext = await _authenticationService.AuthenticateDataPlaneCall(_httpContextAccessor.HttpContext, allowedScopes: ScopesForWriteLakehouseFile);
            var lakeHouseAccessToken = await _authenticationService.GetAccessTokenOnBehalfOf(authorizationContext, OneLakeScopes);

            var filePath = $"{request.WorkspaceId}/{request.LakehouseId}/Files/{request.FileName}";

            var fileExists = await _lakeHouseClientService.CheckIfFileExists(lakeHouseAccessToken, filePath);

            if (fileExists && !request.OverwriteIfExists)
            {
                // File exists, and overwrite is not allowed, return an appropriate response
                _logger.LogError($"WriteToLakehouseFile failed. The file already exists at filePath: {filePath}.");
                return Conflict("File already exists. Overwrite is not allowed.");
            }

            // The WriteToLakehouseFile method creates a new item if it doesn't exist,
            // but if it already exists and overwrite is allowed, it deletes the existing one and then creates a new one and writes content to it.
            await _lakeHouseClientService.WriteToLakehouseFile(lakeHouseAccessToken, filePath, request.Content);

            _logger.LogInformation($"WriteToLakehouseFile succeeded for filePath: {filePath}");
            return Ok();
        }

        [HttpGet("onelake/{workspaceId:guid}/{lakehouseId:guid}/tables")]
        public async Task<IActionResult> GetTablesAsync(Guid workspaceId, Guid lakehouseId)
        {
            var authorizationContext = await _authenticationService.AuthenticateDataPlaneCall(_httpContextAccessor.HttpContext, allowedScopes: ScopesForReadLakehouseFile);
            var token = await _authenticationService.GetAccessTokenOnBehalfOf(authorizationContext, OneLakeScopes);
            var tables = await _lakeHouseClientService.GetOneLakeTables(token, workspaceId, lakehouseId);
            return Ok(tables);
        }



        [HttpGet("onelake/{workspaceId:guid}/{lakehouseId:guid}/files")]
        public async Task<IActionResult> GetFilesAsync(Guid workspaceId, Guid lakehouseId)
        {
            var authorizationContext = await _authenticationService.AuthenticateDataPlaneCall(_httpContextAccessor.HttpContext, allowedScopes: ScopesForReadLakehouseFile);
            var token = await _authenticationService.GetAccessTokenOnBehalfOf(authorizationContext, OneLakeScopes);
            var tables = await _lakeHouseClientService.GetOneLakeFiles(token, workspaceId, lakehouseId);
            return Ok(tables);
        }

        [HttpGet("onelake/{workspaceId:guid}/{lakehouseId:guid}/filePreview")]
        public async Task<IActionResult> GetFilePreviewAsync(Guid workspaceId, Guid lakehouseId, string path)
        {
            var authorizationContext = await _authenticationService.AuthenticateDataPlaneCall(_httpContextAccessor.HttpContext, allowedScopes: ScopesForReadLakehouseFile);

            var credential = await _authenticationService.GetTokenCredential(authorizationContext);


            var accountName = "onelake";
            var dfsUri = $"https://{accountName}.dfs.fabric.microsoft.com";

            var fabricClient = new FabricClient(credential);
            var workspace = await fabricClient.Core.Workspaces.GetWorkspaceAsync(workspaceId);
            var lakehouse = await fabricClient.Lakehouse.Items.GetLakehouseAsync(workspaceId, lakehouseId);
            var dataLakeServiceClient = new DataLakeServiceClient(
                new Uri(dfsUri),
                credential);
            var fileSystemClient = dataLakeServiceClient.GetFileSystemClient(workspace.Value.DisplayName);
            var directoryClient = fileSystemClient.GetDirectoryClient($"{lakehouse.Value.DisplayName}.Lakehouse");
            var fileName = Path.GetFileName(path);
            var directory = path[..^(fileName.Length + 1)];
            directoryClient = directoryClient.GetSubDirectoryClient(directory);
            var fileClient = directoryClient.GetFileClient(Path.GetFileName(path));
            await using var fileStream = await fileClient.OpenReadAsync();
            using var streamReader = new StreamReader(fileStream);
            using var csv = new CsvReader(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                BadDataFound = null,
            });
            csv.Context.RegisterClassMap<MyClassWithDictionaryMapper>();

            var batch = new List<Dictionary<string, string>>();
            await foreach (var currentRecord in csv.GetRecordsAsync<CsvRow>())
            {
                batch.Add(currentRecord.Columns);
                if (batch.Count >= MaxPreviewRows)
                {
                    break;
                }
            }

            return new OkObjectResult(batch);
        }

        [HttpGet("/itemJob/{workspaceId:guid}/{itemId:guid}/{jobId:guid}")]
        public async Task<IActionResult> TestJobs(Guid workspaceId, Guid itemId, Guid jobId)
        {
            var authorizationContext = await _authenticationService.AuthenticateDataPlaneCall(_httpContextAccessor.HttpContext, allowedScopes: ScopesForReadLakehouseFile);

            var credential = await _authenticationService.GetTokenCredential(authorizationContext);
            var fabricClient = new FabricClient(credential);
            var instance = await fabricClient.Core.JobScheduler.GetItemJobInstanceAsync(workspaceId, itemId, jobId);
            // TODO: 404
            return Ok(new RunningJob(instance.Value.Id, instance.Value.StartTimeUtc, instance.Value.Status?.ToString(), instance.Value.JobType));
        }

        public record RunningJob(Guid? Id, string StartTimeUtc, string Status, string JobType);

        [HttpPost("connections/test")]
        public async Task<IActionResult> TestConnectionAsync([FromBody] CluedInConnection connection)
        {
            try
            {
                var requestUri = new Uri($"https://{connection.OrganizationName}.{connection.Domain}/auth/connect/token");

                var client = HttpClientFactory.CreateClient(IngestionConstants.AllowUntrustedSSLClient);

                var requestBody = new Dictionary<string, string>
                {
                    ["username"] = connection.UserEmail,
                    ["password"] = connection.UserPassword,
                    ["grant_type"] = "password",
                    ["client_id"] = connection.OrganizationName,
                };

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = new FormUrlEncodedContent(requestBody),
                };

                var response = await client.SendAsync(requestMessage, CancellationToken.None).ConfigureAwait(false);

                var result = await response.Content
                    .DeserializeToAnonymousTypeAsync(new { access_token = "" })
                    .ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid result because it is empty.");

                if (string.IsNullOrWhiteSpace(result?.access_token))
                {
                    return Ok(new TestConnectionResult(false, "Invalid user email or password"));
                }
                return Ok(new TestConnectionResult(true, string.Empty));
            }
            catch(Exception ex)
            {
                return Ok(new TestConnectionResult(false, ex.Message));
            }
        }

        public class CluedInConnection(
            string Domain,
            string OrganizationName,
            string UserEmail,
            string UserPassword)
        {
            [JsonProperty("domain")]
            public string Domain { get; } = Domain;
            public string OrganizationName { get; } = OrganizationName;
            public string UserEmail { get; } = UserEmail;
            public string UserPassword { get; } = UserPassword;
        }
        public record TestConnectionResult(bool IsValid, string message);
    }
}