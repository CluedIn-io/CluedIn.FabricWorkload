// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Fabric_Extension_BE_Boilerplate.Services.Ingestion;

internal class FileSource
{
    public int DataSourceSetId { get; set; }

    public int DataSourceId { get; set; }

    public Guid DataSetId { get; set; }

    public Guid FileId { get; set; }

    public int AnnotationId { get; set; }

    public Guid ConnectorConfigurationId { get; set; }

    public string UploadFilePath { get; set; }

    public string VocabularyName { get; set; }

    public Guid VocabularyId { get; set; }

    //public Dictionary<string, CustomVocabularyMappingEntry> CustomVocabulariesMapping { get; set; } = new();

    public string EntityType { get; set; }

    //public Dictionary<string, string> CustomEntityTypesMapping { get; set; } = new();

    public bool IsExternalUploadFilePath { get; set; }

    public string EntityTypeRoute => EntityType.ToLowerInvariant();

    public Dictionary<string, string> VocabularyKeyToAnnotationKeyMapping { get; set; } = new();
    public IList<KeyValuePair<string, string>> ColumnToVocabularyKeyMapping { get; set; } = [];
}