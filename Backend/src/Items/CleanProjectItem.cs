// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Boilerplate.Constants;
using Boilerplate.Contracts;
using Boilerplate.Controllers;
using Boilerplate.Exceptions;
using Boilerplate.Services;
using Boilerplate.Utils;
using Fabric_Extension_BE_Boilerplate.Constants;
using Fabric_Extension_BE_Boilerplate.Contracts.FabricAPI.Workload;
using Fabric_Extension_BE_Boilerplate.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Fabric.Api.Notebook.Models;
using Microsoft.Fabric.Api;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Fabric.Api.Core.Models;
using System.Text.Json;
using System.Text;
using Microsoft.Fabric.Api.Admin.Models;
using System.Linq;
using Status = Microsoft.Fabric.Api.Core.Models.Status;

namespace Boilerplate.Items
{
    public class CleanProjectItem : ItemBase<CleanProjectItem, CleanProjectItemMetadata, CleanProjectItemClientMetadata>, ICleanProjectItem
    {
        private static readonly IList<string> OneLakeScopes = new[] { $"{EnvironmentConstants.OneLakeResourceId}/.default" };

        private static readonly IList<string> FabricScopes = new[] { $"{EnvironmentConstants.FabricBackendResourceId}/Lakehouse.Read.All" };

        private readonly ILakehouseClientService _lakeHouseClientService;

        private readonly IAuthenticationService _authenticationService;
        private readonly ILoggerFactory loggerFactory;
        private readonly IItemMetadataStore _itemMetadataStore;
        private readonly IHttpClientFactory httpClientFactory;
        private CleanProjectItemMetadata _metadata;

        public CleanProjectItem(
            ILogger<CleanProjectItem> logger,
            ILoggerFactory loggerFactory,
            IItemMetadataStore itemMetadataStore,
            IHttpClientFactory httpClientFactory,
            ILakehouseClientService lakeHouseClientService,
            IAuthenticationService authenticationService,
            AuthorizationContext authorizationContext)
            : base(logger, itemMetadataStore, authorizationContext)
        {
            _lakeHouseClientService = lakeHouseClientService;
            _authenticationService = authenticationService;
            this.loggerFactory = loggerFactory;
            _itemMetadataStore = itemMetadataStore;
            this.httpClientFactory = httpClientFactory;
        }

        public override string ItemType => WorkloadConstants.ItemTypes.CleanProjectItem;

        public ItemReference InputFileLakehouse => Metadata.InputFileLakehouse;
        public ItemReference OutputFileLakehouse => Metadata.OutputFileLakehouse;
        public string InputFilePath => Metadata.InputFilePath;
        public string OutputFilePath => Metadata.OutputFilePath;
        public string OutputFileFormat => Metadata.OutputFileFormat;
        public string OrganizationName => Metadata.OrganizationName;
        public string Domain => Metadata.Domain;
        public string UserEmail => Metadata.UserEmail;
        public string UserPassword => Metadata.UserPassword;
        public string MappingJson => Metadata.MappingJson;
        public string NotebookId => Metadata.NotebookId;
        public string CleanProjectId => Metadata.CleanProjectId;
        public string CurrentStatus => Metadata.CurrentStatus;
        public Guid SetupCleanProjectJobId => Metadata.SetupCleanProjectJobId;
        public Guid CleanInFabricJobId => Metadata.CleanInFabricJobId;

        public override async Task<ItemPayload> GetItemPayload()
        {
            var typeSpecificMetadata = GetTypeSpecificMetadata();

            return new ItemPayload
            {
                CleanProjectItemMetadata = typeSpecificMetadata.ToClientMetadata(
                    await GetLakehouseFabricItem(typeSpecificMetadata.InputFileLakehouse),
                    await GetLakehouseFabricItem(typeSpecificMetadata.OutputFileLakehouse),
                    typeSpecificMetadata.InputFilePath,
                    typeSpecificMetadata.OutputFilePath,
                    typeSpecificMetadata.OutputFileFormat,
                    typeSpecificMetadata.OrganizationName,
                    typeSpecificMetadata.Domain,
                    typeSpecificMetadata.UserEmail,
                    typeSpecificMetadata.UserPassword,
                    typeSpecificMetadata.MappingJson,
                    typeSpecificMetadata.NotebookId,
                    typeSpecificMetadata.CleanProjectId,
                    typeSpecificMetadata.CurrentStatus,
                    typeSpecificMetadata.SetupCleanProjectJobId,
                    typeSpecificMetadata.CleanInFabricJobId),
            };

            async Task<FabricItem> GetLakehouseFabricItem(ItemReference reference)
            {
                if (reference != null && reference.Id != Guid.Empty)
                {
                    try
                    {
                        var token = await _authenticationService.GetAccessTokenOnBehalfOf(AuthorizationContext, FabricScopes);
                        return await _lakeHouseClientService.GetFabricLakehouse(token, reference.WorkspaceId, reference.Id);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to retrieve FabricLakehouse for lakehouse: {reference.Id} in workspace: {reference.WorkspaceId}. Error: {ex.Message}");
                    }
                }

                return null;
            }
        }

        public record CleanItemStatus (string JobType, string Step, string Message);
        public override async Task ExecuteJob(string jobType, Guid jobInstanceId, JobInvokeType invokeType, CreateItemJobInstancePayload creationPayload)
        {
            var token = await _authenticationService.GetAccessTokenOnBehalfOf(AuthorizationContext, OneLakeScopes);

            var typeSpecificMetadata = GetTypeSpecificMetadata();
            if (jobType == CleanProjectItemJobType.SetupCleanProject)
            {
                try
                {
                    Metadata.CurrentStatus = JsonSerializer.Serialize(new CleanItemStatus(jobType, "Initializing", null));
                    Metadata.SetupCleanProjectJobId = jobInstanceId;
                    await SaveChanges();
                    if (Guid.TryParse(CleanProjectId, out var parsedCleanProjectId) && parsedCleanProjectId != Guid.Empty)
                    {

                        var ingestion = new IngestionEndpointOperation(
                                                    loggerFactory.CreateLogger<IngestionEndpointOperation>(),
                                                    httpClientFactory,
                                                    this,
                                                    _authenticationService,
                                                    AuthorizationContext);

                        var exists = await ingestion.GetCleaningProjectExistsAsync(parsedCleanProjectId, CancellationToken.None);
                        if (!exists)
                        {
                            Logger.LogInformation("Clean project is missing. Reingesting.");
                            this.Metadata.CleanProjectId = Guid.Empty.ToString();
                            await this.SaveChanges();
                            await IngestAndCreateCleanProjectAsync();
                        }
                        else
                        {
                            Logger.LogInformation("Clean project is present. Skipping.");
                        }
                    }
                    else
                    {
                        Logger.LogInformation("Clean project has not been created before. Creating.");
                        await IngestAndCreateCleanProjectAsync();
                    }

                    if (Guid.TryParse(NotebookId, out var parsedNotebookId) && parsedNotebookId != Guid.Empty)
                    {
                        var id = await UpdateNotebookAsync(typeSpecificMetadata);
                        if (id == null)
                        {
                            Logger.LogInformation("Notebook is missing. Recreating.");
                            this.Metadata.NotebookId = Guid.Empty.ToString();
                            await this.SaveChanges();
                            var notebookId = await CreateNotebookAsync(typeSpecificMetadata);
                            this.Metadata.NotebookId = notebookId.ToString();
                        }
                    }
                    else
                    {
                        Logger.LogInformation("Notebook was not created before. Creating.");
                        await CreateNotebookLocalAsync(typeSpecificMetadata);
                    }
                    Metadata.CurrentStatus = string.Empty;
                    await this.SaveChanges();
                }
                catch (Exception ex)
                {
                    Metadata.CurrentStatus = JsonSerializer.Serialize(new CleanItemStatus(jobType, "Failed", ex.Message));
                    await this.SaveChanges();
                    Logger.LogError(ex, "Failed to run ingestion.");
                    throw;
                }
            }
            else if (jobType == CleanProjectItemJobType.CleanInFabric)
            {
                Metadata.CurrentStatus = JsonSerializer.Serialize(new CleanItemStatus(jobType, "Initializing", null));
                Metadata.CleanInFabricJobId = jobInstanceId;
                await SaveChanges();
                try
                {
                    var jobId = await RunNotebookAsync(typeSpecificMetadata, new Guid(typeSpecificMetadata.NotebookId));
                    Metadata.CurrentStatus = JsonSerializer.Serialize(new CleanItemStatus(jobType, "Running", jobId));
                    await SaveChanges();
                }
                catch (Exception ex)
                {
                    Metadata.CurrentStatus = string.Empty;
                    await SaveChanges();
                    throw;
                }
            }

            async Task IngestAndCreateCleanProjectAsync()
            {
                var ingestion = new IngestionEndpointOperation(
                                            loggerFactory.CreateLogger<IngestionEndpointOperation>(),
                                            httpClientFactory,
                                            this,
                                            _authenticationService,
                                            AuthorizationContext);
                var (cleanProjectId, mappingJson) = await ingestion.Setup(CancellationToken.None);
                this.Metadata.CleanProjectId = cleanProjectId.ToString();
                this.Metadata.MappingJson = mappingJson;
            }

            async Task CreateNotebookLocalAsync(CleanProjectItemMetadata typeSpecificMetadata)
            {
                var notebookId = await CreateNotebookAsync(typeSpecificMetadata);
                this.Metadata.NotebookId = notebookId.ToString();
            }
        }

        private async Task<string> RunNotebookAsync(CleanProjectItemMetadata typeSpecificMetadata, Guid notebookId)
        {
            var credential = await _authenticationService.GetTokenCredential(AuthorizationContext);
            var fabricClient = new FabricClient(credential);
            var inputLakehouseWorkspace = await fabricClient.Core.Workspaces.GetWorkspaceAsync(InputFileLakehouse.WorkspaceId);
            var outputLakehouseWorkspace = InputFileLakehouse.WorkspaceId == OutputFileLakehouse.WorkspaceId
                ? inputLakehouseWorkspace
                : await fabricClient.Core.Workspaces.GetWorkspaceAsync(OutputFileLakehouse.WorkspaceId);
            var inputLakehouse = await fabricClient.Lakehouse.Items.GetLakehouseAsync(InputFileLakehouse.WorkspaceId, typeSpecificMetadata.InputFileLakehouse.Id);
            var outputLakehouse = InputFileLakehouse.Id == OutputFileLakehouse.Id
                ? inputLakehouse
                : await fabricClient.Lakehouse.Items.GetLakehouseAsync(OutputFileLakehouse.WorkspaceId, typeSpecificMetadata.OutputFileLakehouse.Id);

            var executionData = new Dictionary<string, object>
            {
                ["parameters"] = new Dictionary<string, object>
                {
                    ["Domain"] = new Dictionary<string, object>
                    {
                        ["value"] = typeSpecificMetadata.Domain,
                        ["type"] = "string",
                    },
                    ["OrganizationName"] = new Dictionary<string, object>
                    {
                        ["value"] = typeSpecificMetadata.OrganizationName,
                        ["type"] = "string",
                    },
                    ["UserEmail"] = new Dictionary<string, object>
                    {
                        ["value"] = typeSpecificMetadata.UserEmail,
                        ["type"] = "string",
                    },
                    ["UserPassword"] = new Dictionary<string, object>
                    {
                        ["value"] = typeSpecificMetadata.UserPassword,
                        ["type"] = "string",
                    },
                    ["InputFilePath"] = new Dictionary<string, object>
                    {
                        ["value"] = $"abfss://{inputLakehouseWorkspace.Value.DisplayName}@onelake.dfs.fabric.microsoft.com/{inputLakehouse.Value.DisplayName}.Lakehouse/{typeSpecificMetadata.InputFilePath}",
                        ["type"] = "string",
                    },
                    ["OutputFilePath"] = new Dictionary<string, object>
                    {
                        ["value"] = $"abfss://{outputLakehouseWorkspace.Value.DisplayName}@onelake.dfs.fabric.microsoft.com/{outputLakehouse.Value.DisplayName}.Lakehouse/{typeSpecificMetadata.OutputFilePath}",
                        ["type"] = "string",
                    },
                    ["MappingJson"] = new Dictionary<string, object>
                    {
                        ["value"] = MappingJson,
                        ["type"] = "string",
                    },
                    ["CleanProjectId"] = new Dictionary<string, object>
                    {
                        ["value"] = CleanProjectId,
                        ["type"] = "string",
                    },
                }
            };

            var demand = await fabricClient.Core.JobScheduler.RunOnDemandItemJobAsync(
                WorkspaceObjectId,
                notebookId,
                "RunNotebook",
                new ()
                {
                    ExecutionData = executionData
                });
            if (demand.Headers.TryGetValue("Location", out var location))
            {
                var lastSlashIndex = location.LastIndexOf("/");
                var jobId = location.Substring(lastSlashIndex + 1);
                return jobId;
            }
            return null;
        }
        private async Task<Guid?> UpdateNotebookAsync(CleanProjectItemMetadata typeSpecificMetadata)
        {
            var credential = await _authenticationService.GetTokenCredential(AuthorizationContext);
            var fabricClient = new FabricClient(credential);
            var workspace = await fabricClient.Core.Workspaces.GetWorkspaceAsync(WorkspaceObjectId);

            var notebook = await fabricClient.Notebook.Items.GetNotebookAsync(
                Metadata.InputFileLakehouse.WorkspaceId,
                new Guid(Metadata.NotebookId));

            if (notebook != null)
            {
                Logger.LogInformation("Notebook: {NotebookId} exists. Updating.", notebook.Value.Id);
                var updateRequest = new UpdateNotebookDefinitionRequest(new NotebookDefinition(
                    [
                        new ()
                        {
                            Path = "notebook-content.py",
                            Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(GetNotebookContents())),
                            PayloadType = PayloadType.InlineBase64,
                        },
                        new ()
                        {
                            Path = ".platform",
                            Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(GetNotebookPlatform())),
                            PayloadType = PayloadType.InlineBase64,
                        },
                    ]));
                await fabricClient.Notebook.Items.UpdateNotebookDefinitionAsync(WorkspaceObjectId, new Guid(Metadata.NotebookId), updateRequest);
                Logger.LogInformation("Notebook: {NotebookId} updated.", notebook.Value.Id);
                return notebook.Value.Id.Value;
            }

            return null;
        }
        private async Task<Guid> CreateNotebookAsync(CleanProjectItemMetadata typeSpecificMetadata)
        {
            var credential = await _authenticationService.GetTokenCredential(AuthorizationContext);
            var fabricClient = new FabricClient(credential);
            var workspace = await fabricClient.Core.Workspaces.GetWorkspaceAsync(WorkspaceObjectId);

            var notebook = await fabricClient.Notebook.Items.CreateNotebookAsync(
                Metadata.InputFileLakehouse.WorkspaceId,
                new CreateNotebookRequest($"{this.DisplayName}_CluedInCleanse_{ItemObjectId}")
                {
                    Definition = new (
                    [
                        new ()
                        {
                            Path = "notebook-content.py",
                            Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(GetNotebookContents())),
                            PayloadType = PayloadType.InlineBase64,
                        },
                        new ()
                        {
                            Path = ".platform",
                            Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(GetNotebookPlatform())),
                            PayloadType = PayloadType.InlineBase64,
                        },
                    ])
                });
            return notebook.Value.Id.Value;
        }

        public override async Task<ItemJobInstanceState> GetJobState(string jobType, Guid jobInstanceId)
        {
            try
            {
                return await GetJobStateLocal(jobType, jobInstanceId);
            }
            catch (Exception exc)
            {
                Logger.LogError(exc, "Failed to get job state.");
                throw;
            }

            async Task<ItemJobInstanceState> GetJobStateLocal(string jobType, Guid jobInstanceId)
            {
                Logger.LogDebug("Begin getting job state for {JobType} and {JobId}", jobType, jobInstanceId);
                var token = await _authenticationService.GetAccessTokenOnBehalfOf(AuthorizationContext, OneLakeScopes);

                Logger.LogDebug("Check if job cancel request exists {JobType} and {JobId}", jobType, jobInstanceId);

                if (_itemMetadataStore.JobCancelRequestExists(TenantObjectId, ItemObjectId, jobInstanceId))
                {
                    return new ItemJobInstanceState { Status = JobInstanceStatus.Cancelled };
                }


                if (jobType == CleanProjectItemJobType.SetupCleanProject)
                {
                    //var filePath = GetLakehouseFilePath(jobType, jobInstanceId);
                    //var fileExists = await _lakeHouseClientService.CheckIfFileExists(token, filePath);
                    var typeSpecificMetadata = GetTypeSpecificMetadata();
                    var isValidCleanProjectId = Guid.TryParse(CleanProjectId, out var parsedCleanProjectId) && parsedCleanProjectId != Guid.Empty;
                    var isValidNotebookId = Guid.TryParse(NotebookId, out var parsedNotebookId) && parsedNotebookId != Guid.Empty;
                    var isIdle = CurrentStatus == string.Empty;

                    Logger.LogDebug("Check status {JobType} and {JobId}", jobType, jobInstanceId);
                    if (!string.IsNullOrWhiteSpace(CurrentStatus))
                    {
                        Logger.LogDebug("Deserializing status {JobType} and {JobId}, {CurrentStatus}", jobType, jobInstanceId, CurrentStatus);
                        var status = JsonSerializer.Deserialize<CleanItemStatus>(CurrentStatus);
                        Logger.LogDebug("Deserialized status {JobType} and {JobId}, {CurrentStatus}", jobType, jobInstanceId, CurrentStatus);
                        if (status.Step == "Failed")
                        {
                            return new ItemJobInstanceState
                            {
                                Status = JobInstanceStatus.Failed,
                                ErrorDetails = new ErrorDetails()
                                {
                                    Message = status.Message,
                                    ErrorCode = ErrorCodes.InternalError,
                                }
                            };

                        }
                    }
                    Logger.LogDebug("Check status {JobType} and {JobId}", jobType, jobInstanceId);
                    return new ItemJobInstanceState
                    {
                        Status = isValidCleanProjectId && isValidNotebookId && isIdle ? JobInstanceStatus.Completed : JobInstanceStatus.InProgress,
                    };
                }
                else if (jobType == CleanProjectItemJobType.CleanInFabric)
                {
                    var isValidNotebookId = Guid.TryParse(NotebookId, out var parsedNotebookId) && parsedNotebookId != Guid.Empty;
                    if (!isValidNotebookId)
                    {
                        return new ItemJobInstanceState
                        {
                            Status = JobInstanceStatus.Failed,
                        };
                    }

                    Logger.LogDebug("Deserializing status {JobType} and {JobId}, {CurrentStatus}", jobType, jobInstanceId, CurrentStatus);
                    var status = JsonSerializer.Deserialize<CleanItemStatus>(CurrentStatus);
                    Logger.LogDebug("Deserialized status {JobType} and {JobId}, {CurrentStatus}", jobType, jobInstanceId, CurrentStatus);

                    if (status.Step == "Running")
                    {
                        if (Guid.TryParse(status.Message, out var notebookRunId) && notebookRunId != Guid.Empty)
                        {
                            var credential = await _authenticationService.GetTokenCredential(AuthorizationContext);
                            var fabricClient = new FabricClient(credential);
                            var notebookJobInstance = await fabricClient.Core.JobScheduler.GetItemJobInstanceAsync(WorkspaceObjectId, parsedNotebookId, notebookRunId);
                            var latestJobStatus = notebookJobInstance?.Value?.Status;

                            if (latestJobStatus == Status.Completed)
                            {

                                return new ItemJobInstanceState
                                {
                                    Status = JobInstanceStatus.Completed,
                                };
                            }

                            if (latestJobStatus == Status.Failed)
                            {

                                return new ItemJobInstanceState
                                {
                                    Status = JobInstanceStatus.Failed,
                                };
                            }

                            if (latestJobStatus == Status.Cancelled)
                            {

                                return new ItemJobInstanceState
                                {
                                    Status = JobInstanceStatus.Cancelled,
                                };
                            }

                            if (latestJobStatus == Status.InProgress)
                            {

                                return new ItemJobInstanceState
                                {
                                    Status = JobInstanceStatus.InProgress,
                                };
                            }
                            return new ItemJobInstanceState
                            {
                                Status = JobInstanceStatus.InProgress,
                            };
                        }

                        return new ItemJobInstanceState
                        {
                            Status = JobInstanceStatus.Failed,
                        };
                    }

                    return new ItemJobInstanceState
                    {
                        Status = JobInstanceStatus.NotStarted,
                    };


                }
                return new ItemJobInstanceState
                {
                    Status = JobInstanceStatus.Completed,
                };
            }
        }

        private string GetLakehouseFilePath(string jobType, Guid jobInstanceId)
        {
            var typeToFileName = new Dictionary<string, string>
            {
                { CleanProjectItemJobType.SetupCleanProject, $"CalculationResultSetupCleanProject_{jobInstanceId}.txt" },
                { CleanProjectItemJobType.CleanInFabric, $"CalculationResultCleanInFabric_{jobInstanceId}.txt" },
                { CleanProjectItemJobType.ScheduledJob, $"CalculationResult_{jobInstanceId}.txt" },
                { CleanProjectItemJobType.CalculateAsText, $"CalculationResult_{jobInstanceId}.txt" },
                { CleanProjectItemJobType.LongRunningCalculateAsText, $"CalculationResult_{jobInstanceId}.txt" },
                { CleanProjectItemJobType.CalculateAsParquet, $"CalculationResult_{jobInstanceId}.parquet" }
            };
            typeToFileName.TryGetValue(jobType, out var fileName);

            if (fileName != null)
            {
                return $"{_metadata.OutputFileLakehouse.WorkspaceId}/{_metadata.OutputFileLakehouse.Id}/Files/{fileName}";
            }
            throw new NotSupportedException("Workload job type is not supported");
        }

        private CleanProjectItemMetadata Metadata => Ensure.NotNull(_metadata, "The item object must be initialized before use");

        public string CluedInCleanProjectLink { get; set; }

        public Guid CluedInInstanceId { get; set; }

        private void ValidateOperandsBeforeDouble(int operand1, int operand2)
        {
            var invalidOperands = new List<string>();
            if (operand1 > int.MaxValue / 2 || operand1 < int.MinValue / 2)
            {
                invalidOperands.Add("Operand1");
            }
            if (operand2 > int.MaxValue / 2 || operand2 < int.MinValue / 2)
            {
                invalidOperands.Add("Operand2");
            }
            if (!invalidOperands.IsNullOrEmpty())
            {
                string joinedInvalidOperands = string.Join(", ", invalidOperands);
                throw new DoubledOperandsOverflowException(new List<string> { joinedInvalidOperands });
            }
        }

        protected override void SetDefinition(CreateItemPayload payload)
        {
            if (payload == null)
            {
                Logger.LogInformation("No payload is provided for {0}, objectId={1}", ItemType, ItemObjectId);
                _metadata = CleanProjectItemMetadata.Default.Clone();
                return;
            }

            if (payload.CleanProjectItemMetadata == null)
            {
                throw new InvalidItemPayloadException(ItemType, ItemObjectId);
            }


            if (payload.CleanProjectItemMetadata.InputFileLakehouse == null)
            {
                throw new InvalidItemPayloadException(ItemType, ItemObjectId)
                    .WithDetail(ErrorCodes.ItemPayload.MissingLakehouseReference, "Missing InputFileLakehouse reference");
            }

            if (payload.CleanProjectItemMetadata.OutputFileLakehouse == null)
            {
                throw new InvalidItemPayloadException(ItemType, ItemObjectId)
                    .WithDetail(ErrorCodes.ItemPayload.MissingLakehouseReference, "Missing OutputFileLakehouse reference");
            }

            _metadata = payload.CleanProjectItemMetadata.Clone();
        }

        protected override void UpdateDefinition(UpdateItemPayload payload)
        {
            if (payload == null)
            {
                Logger.LogInformation("No payload is provided for {0}, objectId={1}", ItemType, ItemObjectId);
                return;
            }

            if (payload.CleanProjectItemMetadata == null)
            {
                throw new InvalidItemPayloadException(ItemType, ItemObjectId);
            }

            if (payload.CleanProjectItemMetadata.InputFileLakehouse == null)
            {
                throw new InvalidItemPayloadException(ItemType, ItemObjectId)
                    .WithDetail(ErrorCodes.ItemPayload.MissingLakehouseReference, "Missing InputFileLakehouse reference");
            }

            if (payload.CleanProjectItemMetadata.OutputFileLakehouse == null)
            {
                throw new InvalidItemPayloadException(ItemType, ItemObjectId)
                    .WithDetail(ErrorCodes.ItemPayload.MissingLakehouseReference, "Missing OutputFileLakehouse reference");
            }


            payload.CleanProjectItemMetadata.CleanProjectId = Metadata.CleanProjectId;
            payload.CleanProjectItemMetadata.CurrentStatus = Metadata.CurrentStatus;
            payload.CleanProjectItemMetadata.NotebookId = Metadata.NotebookId;
            payload.CleanProjectItemMetadata.SetupCleanProjectJobId = Metadata.SetupCleanProjectJobId;
            payload.CleanProjectItemMetadata.CleanInFabricJobId = Metadata.CleanInFabricJobId;
            SetTypeSpecificMetadata(payload.CleanProjectItemMetadata);
        }

        protected override void SetTypeSpecificMetadata(CleanProjectItemMetadata itemMetadata)
        {
            _metadata = itemMetadata.Clone();
        }

        protected override CleanProjectItemMetadata GetTypeSpecificMetadata()
        {
            return Metadata.Clone();
        }

        protected string GetNotebookPlatform()
        {
            return $$"""
            {
                "$schema": "https://developer.microsoft.com/json-schemas/fabric/gitIntegration/platformProperties/2.0.0/schema.json",
                "metadata": {
                "type": "Notebook",
                "displayName": "TestNotebook",
                "description": "New notebook"
                },
                "config": {
                "version": "2.0",
                "logicalId": "00000000-0000-0000-0000-000000000000"
                }
            }
            """;
        }

        protected string GetNotebookContents()
        {
            return $$""""
            # Fabric notebook source

            # METADATA ********************

            # META {
            # META   "kernel_info": {
            # META     "name": "synapse_pyspark"
            # META   },
            # META   "dependencies": {
            # META     "lakehouse": {
            # META       "default_lakehouse_name": "",
            # META       "default_lakehouse_workspace_id": ""
            # META     }
            # META   }
            # META }

            # PARAMETERS CELL ********************

            Domain = "[placeholder]example.com"
            OrganizationName = "[placeholder]foobar"
            UserEmail = "[placeholder]myuser@foobar.com"
            UserPassword = "[placeholder]mypassword"
            InputFilePath = "[placeholder]abfss://MyInputFileWorkspace@onelake.dfs.fabric.microsoft.com/MyInputFileLakehouse.Lakehouse/path/to/my/input/file.ext"
            InputFileFormat = 'csv'
            OutputFilePath = "[placeholder]abfss://MyOutputFileWorkspace@onelake.dfs.fabric.microsoft.com/MyOutputFileLakehouse.Lakehouse/path/to/my/output/file.ext"
            OutputFileFormat = 'csv'
            MappingJson='[placeholder]{"ColumnA":"vocabA.fileColumnA","ColumnB":"vocabA.fileColumnB"}'
            OutputFileWritingMode = "overwrite"
            CleanProjectId = "[placeholder]00000000-0000-0000-0000-000000000000"

            # METADATA ********************

            # META {
            # META   "language": "python",
            # META   "language_group": "synapse_pyspark"
            # META }

            # CELL ********************

            !pip install cluedin
            !pip install jqqb_evaluator
            !pip install chardet

            import cluedin
            import json
            import pandas as pd
            import chardet

            def main():
                ctx = cluedin.Context.from_dict({
                    "domain": Domain,
                    "org_name": OrganizationName,
                    "user_email": UserEmail,
                    "user_password": UserPassword
                })
                print("Begin cleaning data.")
                print("Getting token from domain: '" + Domain + "', Org: '" + OrganizationName + "'" + ", Email: '" + UserEmail + "'.")
                token = ctx.get_token()
                print("Got token from, begin processing.")

                # Mount file and transform paths to local paths
                inputMountName = "/tmp_input_mnt"
                outputMountName = "/tmp_output_mnt"
                inputMountPaths = get_paths_for_mount(InputFilePath)
                outputMountPaths = get_paths_for_mount(OutputFilePath)
                mssparkutils.fs.mount(inputMountPaths["basePath"], inputMountName)
                mssparkutils.fs.mount(outputMountPaths["basePath"], outputMountName)
                mount_points = mssparkutils.fs.mounts()
                inputLocalBasePath = next((mp["localPath"] for mp in mount_points if mp["mountPoint"] == inputMountName), None)
                outputLocalBasePath = next((mp["localPath"] for mp in mount_points if mp["mountPoint"] == outputMountName), None)

                inputLocalPath = inputLocalBasePath + inputMountPaths["relativePath"]
                outputLocalPath = outputLocalBasePath + outputMountPaths["relativePath"]
                print("Using input file path '" + InputFilePath + "' transformed to local path '" + inputLocalPath + "'.")
                print("Using output file path '" + OutputFilePath + "' transformed to local path '" + outputLocalPath + "'.")

                print("Begin get rules for cleaning project.")
                cleaningRules = get_cleaning_rules(ctx, CleanProjectId)['data']['preparation']['allCleanProjectRules']['rules']
                activeRules = [rule for rule in cleaningRules if rule["isActive"]]
                print("Total rules: " + str(len(cleaningRules)) + ", Active rules: " + str(len(activeRules)))
                rule_ids = list(map(lambda x: x['id'], activeRules))

                rules = list(map(lambda rule_id: get_rule2(ctx, rule_id), rule_ids))
                evaluators = list(map(lambda rule: cluedin.rules.Evaluator(rule['data']['management']['rule']['condition']), rules))
                actions_with_evaluators = [action_with_evaluator for rule in rules for action_with_evaluator in get_actions_with_evaluators(rule)]

                # Load the data
                with open(inputLocalPath, 'rb') as f:
                    enc = chardet.detect(f.read())  # or readline if the file is large


                # Load data into pandas DataFrame from "/lakehouse/default/Files/Demo/Movies-5000.csv"
                print("Begin reading file using encoding " + enc['encoding'] + " for file " + InputFilePath)
                df = pd.read_csv(inputLocalPath, encoding = enc['encoding'], encoding_errors="ignore")

                # Transform the columns using desired mapping
                mapping = json.loads(MappingJson)
                inverseMapping = {v: k for k, v in mapping.items()}
                print("Transform column before applying rules using mapping: " + str(mapping) + ".")
                df = df.rename(columns=mapping)

                # Transform using rules
                df = df.apply(lambda row: apply_actions(actions_with_evaluators, row), axis=1)

                # Transform back the columns to the original header
                print("Transform column after applying rules using mapping: " + str(inverseMapping) + ".")
                df = df.rename(columns=inverseMapping)

                # Write output
                print("Writing to file '" + OutputFilePath + "'.")
                df.to_csv(outputLocalPath, index=False)

                print("End cleaning data.")

            def get_paths_for_mount(path):
                domain = "fabric.microsoft.com/"
                index = path.find(domain)
                slashIndex = path.find("/", index + len(domain))
                return { "basePath": path[:slashIndex], "relativePath": path[slashIndex:]}

            def apply_actions(actions_with_evaluators, obj):
              """
              Given a list of actions with evaluators pairs and an object (obj),
              apply action to the object if it passes the corresponding evaluator.
              """
              for action_with_evaluator in actions_with_evaluators:
                  if action_with_evaluator['evaluator'].object_matches_rules(obj):
                      obj = action_with_evaluator['action'](obj)
              return obj

            def get_cleaning_rules(context: cluedin.Context,
                           cleaningProjectId) -> dict:
                """
                Retrieves rules for cleaning project

                Args:
                        context (Context): The context object.
                        cleaningProjectId (string): The cleaning project id.

                Returns:
                        dict: The rules' data.

                """
                query = """
                    query allCleanProjectRules($id: ID!, $page: Int, $pageSize: Int) {
                        preparation {
                            allCleanProjectRules(id: $id, page: $page, pageSize: $pageSize) {
                            total
                            rules {
                                id
                                name
                                order
                                createdBy
                                createdAt
                                isActive
                                __typename
                            }
                            __typename
                            }
                            __typename
                        }
                    }
                    """

                variables = {"id": cleaningProjectId, "page": 1, "page_size": 1000}

                return cluedin.gql.org_gql(context, query, variables)

            def get_rules2(context: cluedin.Context,
                           scope=cluedin.rules.RuleScope.DATA_PART,
                           page_number=1) -> dict:
              """
              Retrieves rules based on the specified parameters.

              Args:
                      context (Context): The context object.
                      scope (RuleScope, optional): The scope of the rules. Defaults to RuleScope.DATA_PART.
                      page_number (int, optional): The page number to retrieve. Defaults to 1.

              Returns:
                      dict: The rules' data.

              """
              query = """
                  query getRules($searchName: String, $isActive: Boolean, $pageNumber: Int, $sortBy: String, $sortDirection: String, $scope: String) {
                      management {
                          id
                          rules(
                              searchName: $searchName
                              isActive: $isActive
                              pageNumber: $pageNumber
                              sortBy: $sortBy
                              sortDirection: $sortDirection
                              scope: $scope
                          ) {
                              total
                              data {
                                  id
                                  name
                                  order
                                  description
                                  isActive
                                  createdBy
                                  modifiedBy
                                  createdAt
                                  modifiedAt
                                  author {
                                      id
                                      username
                                      __typename
                                  }
                                  scope
                                  isReprocessing
                                  __typename
                              }
                              __typename
                          }
                          __typename
                      }
                  }
                  """

              variables = {"scope": scope.value, "pageNumber": page_number}

              return cluedin.gql.org_gql(context, query, variables)


            def get_rule2(context: cluedin.Context, rule_id: str) -> dict:
              """
              Retrieves the properties of a rule based on the provided rule ID.

              Args:
                      context (Context): The context object.
                      rule_id (str): The ID of the rule to retrieve properties for.

              Returns:
                      dict: A dictionary containing the properties of the rule.
              """
              query = """
                  query getRule($id: ID!) {
                      management {
                          id
                          rule(id: $id) {
                              id
                              name
                              description
                              isActive
                              createdBy
                              modifiedBy
                              createdAt
                              modifiedAt
                              condition
                              actions
                              rules
                              sourceDetail {
                                  id
                                  name
                                  type
                                  __typename
                              }
                              author {
                                  id
                                  username
                                  __typename
                              }
                              scope
                              isReprocessing
                              requiresAttention
                              __typename
                          }
                          __typename
                      }
                  }
                  """
              variables = {"id": rule_id}

              return cluedin.gql.org_gql(context, query, variables)

            def set_value_action(obj, field, val):
              """
              Set Value action: takes an object (obj), and sets its property (field) to a value (val).
              """
              obj[field] = val
              return obj

            def get_action(action_json):
              """
              Takes a Rule Action JSON, and returns a lambda
              """
              if action_json['type'] == 'CluedIn.Rules.Actions.SetValue, CluedIn.Rules':
                  field = None
                  val = None
                  for prop in action_json['properties']:
                      if prop['name'] == 'FieldName':
                          field = prop['value']
                      elif prop['name'] == 'Value':
                          val = prop['value']
                  return lambda obj: set_value_action(obj, field, val)

              #print(f'Action "{action_json["type"]}" is not supported. Object:', obj)
              return lambda obj: obj

            def get_actions_with_evaluators(rule):
                """
                For a given rule, returns an iterable of objects containing an action and a corresponding evaluator:
                {
                    'action': lambda x: ...,
                    'evaluator': ...

                }
                """
                for r in rule['data']['management']['rule']['rules']:
                  for a in r['actions']:
                      yield {
                          'evaluator': cluedin.rules.Evaluator(rule['data']['management']['rule']['condition']),
                          'action': get_action(a)
                      }


            main()

            # METADATA ********************

            # META {
            # META   "language": "python",
            # META   "language_group": "synapse_pyspark"
            # META }


            """";
        }
    }
}
