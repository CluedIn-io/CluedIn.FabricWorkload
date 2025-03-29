// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Azure;
using Azure.Data.Tables;

using Boilerplate.Contracts;
using Boilerplate.Exceptions;
using Fabric_Extension_BE_Boilerplate.Constants;
using Fabric_Extension_BE_Boilerplate.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Fabric.Api.Core.Models;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Boilerplate.Services;

/// <summary>
/// This is a naive implementation of a metadata store intended for demonstrating concepts of Fabric workload extensibility.
/// It does not handle many important aspects like concurrency control, transactional updates, encryption at rest and more.
/// </summary>
///
public class TableStorageItemMetadataStore : IItemMetadataStore
{
    private static readonly JsonConverter<Item1Operator> Item1OperatorConverter = new UnknownAsDefaultEnumConverter<Item1Operator>();
    private static readonly JsonSerializerOptions ContentSerializationOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { Item1OperatorConverter },
    };

    private readonly ILogger<TableStorageItemMetadataStore> _logger;
    private readonly string _connectionString;

    public TableStorageItemMetadataStore(ILogger<TableStorageItemMetadataStore> logger )
    {
        _logger = logger;
        // TODO: Config / options
        _connectionString = Environment.GetEnvironmentVariable("TableStorageConnectionString");
    }

    private TableClient GetTableClient(string tableName)
    {
        var tableClient = new TableClient(
            _connectionString,
            tableName);
        return tableClient;
    }

    private TableClient GetItemTableClient()
    {
        return GetTableClient("items");
    }
    private TableClient GetJobCancellationTableClient()
    {
        return GetTableClient("itemJobCancellations");
    }

    public async Task Upsert<TItemMetadata>(Guid tenantObjectId, Guid itemObjectId, CommonItemMetadata commonMetadata, TItemMetadata typeSpecificMetadata)
    {
        var tableEntity = ToItemTableEntity(tenantObjectId, itemObjectId, commonMetadata, typeSpecificMetadata);
        await GetItemTableClient().UpsertEntityAsync(tableEntity);
    }

    public async Task UpsertJobCancel(Guid tenantObjectId, Guid itemObjectId, string jobType, Guid jobInstanceId, ItemJobMetadata itemJobMetadata)
    {
        var tableEntity = ToJobCancellationTableEntity(tenantObjectId, itemObjectId, itemJobMetadata);
        await GetJobCancellationTableClient().UpsertEntityAsync(tableEntity);
    }

    public async Task<ItemMetadata<TItemMetadata>> Load<TItemMetadata>(Guid tenantObjectId, Guid itemObjectId)
    {
        var tableClient = GetItemTableClient();
        var entity = await tableClient.GetEntityAsync<ItemMetadataTableEntity>(tenantObjectId.ToString(), itemObjectId.ToString());

        var commonMetadata = Deserialize<CommonItemMetadata>(entity.Value.CommonMetadataJson);
        var typeSpecificMetadata = Deserialize<TItemMetadata>(entity.Value.TypeSpecificMetadata);
        return new ItemMetadata<TItemMetadata> {  CommonMetadata = commonMetadata, TypeSpecificMetadata = typeSpecificMetadata };
    }

    public bool Exists(Guid tenantObjectId, Guid itemObjectId)
    {
        var tableClient = GetItemTableClient();
        var entity = tableClient.GetEntityIfExists<ItemMetadataTableEntity>(tenantObjectId.ToString(), itemObjectId.ToString());
        return entity.HasValue && entity.Value.TypeSpecificMetadata != null && entity.Value.CommonMetadataJson != null;
    }

    public async Task Delete(Guid tenantObjectId, Guid itemObjectId)
    {
        var tableClient = GetItemTableClient();
        await tableClient.DeleteEntityAsync(tenantObjectId.ToString(), itemObjectId.ToString());
    }

    private string Serialize<TContent>(TContent content)
    {
        return JsonSerializer.Serialize(content, ContentSerializationOptions);
    }
    private TContent Deserialize<TContent>(string json)
    {
        return JsonSerializer.Deserialize<TContent>(json, ContentSerializationOptions);
    }

    private ITableEntity ToJobCancellationTableEntity(Guid tenantObjectId, Guid itemObjectId, ItemJobMetadata itemJobMetadata)
    {
        return new JobCancellationTableEntity
        {
            PartitionKey = JobCancellationTableEntity.GetPartitionKey(tenantObjectId, itemObjectId),
            RowKey = itemJobMetadata.JobInstanceId.ToString(),
            ItemJobMetadataJson = Serialize(itemJobMetadata),
        };
    }

    private ItemMetadataTableEntity ToItemTableEntity<TItemMetadata>(
        Guid tenantObjectId,
        Guid itemObjectId,
        CommonItemMetadata commonMetadata,
        TItemMetadata typeSpecificMetadata)
    {
        return new ItemMetadataTableEntity
        {
            PartitionKey = tenantObjectId.ToString(),
            RowKey = itemObjectId.ToString(),
            CommonMetadataJson = Serialize(commonMetadata),
            TypeSpecificMetadata = Serialize(typeSpecificMetadata),
        };
    }

    public bool JobCancelRequestExists(Guid tenantObjectId, Guid itemObjectId, Guid jobInstanceId)
    {
        var tableClient = GetJobCancellationTableClient();
        var partitionKey = JobCancellationTableEntity.GetPartitionKey(tenantObjectId, itemObjectId);
        var entity = tableClient.GetEntityIfExists<JobCancellationTableEntity>(partitionKey, jobInstanceId.ToString());
        return entity.HasValue && entity.Value.ItemJobMetadataJson != null;
    }



    public class ItemMetadataTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string CommonMetadataJson { get; set; }
        public string TypeSpecificMetadata { get; set; }

    }

    public class JobCancellationTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string ItemJobMetadataJson { get; set; }

        public static string GetPartitionKey(Guid tenantObjectId, Guid itemObjectId) => $"{tenantObjectId}_{itemObjectId}";
    }
}
