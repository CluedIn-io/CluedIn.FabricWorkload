// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Text.Json;

namespace Fabric_Extension_BE_Boilerplate.Services.Ingestion;

internal static class GraphqlQueryHelper
{
    public static string Serialize(string query)
    {
        return JsonSerializer.Serialize(query);
    }
}
