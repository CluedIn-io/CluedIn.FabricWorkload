// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Fabric_Extension_BE_Boilerplate.Services.Ingestion;

internal record ServerUriCollection(
    Uri AuthApiUri,
    Uri PublicApiUri,
    Uri WebApiUri,
    Uri UiGraphqlUri,
    Uri UploadApiUri);
