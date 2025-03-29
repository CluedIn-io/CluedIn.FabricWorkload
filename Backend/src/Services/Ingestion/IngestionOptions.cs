// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Fabric_Extension_BE_Boilerplate.Services.Ingestion;

public class IngestionOptions
{
    public bool UseIngestionEndpoint { get; set; }

    public int IngestionBatchSize { get; set; }

    public int IngestionRequestsDelayInMilliseconds { get; set; }
    public int MaxCleanSize { get; set; }
}
