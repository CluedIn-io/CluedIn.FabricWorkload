// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Fabric_Extension_BE_Boilerplate.Constants
{
    public static class WorkloadConstants
    {
        public static readonly string WorkloadName = Environment.GetEnvironmentVariable("WorkloadName") ?? "CluedInClean.Product";
        //public const string WorkloadName = "CluedIn.Clean";

        public static class ItemTypes
        {
            public static readonly string Item1 = $"{WorkloadName}.SampleWorkloadItem";
            public static readonly string CleanProjectItem = $"{WorkloadName}.CleanProjectItem";
        }
    }
}
