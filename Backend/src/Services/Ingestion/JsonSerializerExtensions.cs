// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fabric_Extension_BE_Boilerplate.Services.Ingestion;

internal static partial class JsonSerializerExtensions
{
    public static async Task<T?> DeserializeToAnonymousTypeAsync<T>(this HttpContent httpContent, T _, JsonSerializerOptions options = null)
    {
        return await httpContent.ReadFromJsonAsync<T>(options).ConfigureAwait(false);
    }

    public static T? DeserializeToAnonymousType<T>(this string json, T _, JsonSerializerOptions options = null)
    {
        return JsonSerializer.Deserialize<T>(json, options);
    }
}
