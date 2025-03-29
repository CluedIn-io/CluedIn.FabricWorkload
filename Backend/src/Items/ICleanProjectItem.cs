// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

using Boilerplate.Contracts;

namespace Boilerplate.Items
{
    public interface ICleanProjectItem : IItem
    {
        ItemReference InputFileLakehouse { get; }

        ItemReference OutputFileLakehouse { get; }

        string InputFilePath { get; }

        string OutputFilePath { get; }

        string CluedInCleanProjectLink { get; }

        Guid CluedInInstanceId { get; }
    }
}
