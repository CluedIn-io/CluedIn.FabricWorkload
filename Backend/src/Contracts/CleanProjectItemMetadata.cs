// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>


using System;

using Boilerplate.Contracts;

public abstract class CleanProjectItemMetadataBase<TLakehouse>
{
    public TLakehouse InputFileLakehouse { get; set; }
    public TLakehouse OutputFileLakehouse { get; set; }
    public string InputFilePath { get; set; }
    public string OutputFilePath { get; set; }
    public string OutputFileFormat { get; set; }
    public string OrganizationName { get; set; }
    public string Domain { get; set; }
    public string UserEmail { get; set; }
    public string UserPassword { get; set; }
    public string MappingJson { get; set; }
    public string NotebookId { get; set; }
    public string CleanProjectId { get; set; }
    public string CurrentStatus { get; set; }
    public Guid SetupCleanProjectJobId { get; set; }
    public Guid CleanInFabricJobId { get; set; }
}
public class CleanProjectItemMetadata : CleanProjectItemMetadataBase<ItemReference>
{

    public static readonly CleanProjectItemMetadata Default = new CleanProjectItemMetadata
    {
        InputFileLakehouse = new ItemReference(),
        OutputFileLakehouse = new ItemReference(),
        InputFilePath = string.Empty,
        OutputFilePath = string.Empty,
        OutputFileFormat = string.Empty,
        OrganizationName = string.Empty,
        Domain = string.Empty,
        UserEmail = string.Empty,
        UserPassword = string.Empty,
        MappingJson = string.Empty,
        NotebookId = string.Empty,
        CleanProjectId = string.Empty,
        CurrentStatus = string.Empty,
        SetupCleanProjectJobId = Guid.Empty,
        CleanInFabricJobId = Guid.Empty,
    };

    public CleanProjectItemMetadata Clone()
    {
        return new CleanProjectItemMetadata
        {
            InputFileLakehouse = InputFileLakehouse,
            OutputFileLakehouse = OutputFileLakehouse,
            InputFilePath = InputFilePath,
            OutputFilePath = OutputFilePath,
            OutputFileFormat = OutputFileFormat,
            OrganizationName = OrganizationName,
            Domain = Domain,
            UserEmail = UserEmail,
            UserPassword = UserPassword,
            MappingJson = MappingJson,
            NotebookId = NotebookId,
            CleanProjectId = CleanProjectId,
            CurrentStatus = CurrentStatus,
            SetupCleanProjectJobId = SetupCleanProjectJobId,
            CleanInFabricJobId = CleanInFabricJobId,
        };
    }

    public CleanProjectItemMetadata ToClientMetadata(
        FabricItem inputFileLakehouse,
        FabricItem outputFileLakehouse,
        string inputFilePath,
        string outputFilePath,
        string outputFileFormat,
        string organizationName,
        string domain,
        string userEmail,
        string userPassword,
        string mappingJson,
        string notebookId,
        string cleanProjectId,
        string currentStatus,
        Guid setupCleanProjectJobId,
        Guid cleanInFabricJobId)
    {
        return new CleanProjectItemMetadata()
        {
            InputFileLakehouse = inputFileLakehouse,
            OutputFileLakehouse = outputFileLakehouse,
            InputFilePath = inputFilePath,
            OutputFilePath = outputFilePath,
            OutputFileFormat = outputFileFormat,
            OrganizationName = organizationName,
            Domain = domain,
            UserEmail = userEmail,
            UserPassword = userPassword,
            MappingJson = mappingJson,
            NotebookId = notebookId,
            CleanProjectId = cleanProjectId,
            CurrentStatus = currentStatus,
            SetupCleanProjectJobId = setupCleanProjectJobId,
            CleanInFabricJobId = cleanInFabricJobId,
        };
    }
}
public class CleanProjectItemClientMetadata : CleanProjectItemMetadataBase<FabricItem> { }