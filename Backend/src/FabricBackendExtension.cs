// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Boilerplate
{
    using Fabric_Extension_BE_Boilerplate.Constants;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System.Threading;
    using System.Threading.Tasks;

    internal class FabricBackendExtension : IHostedService
    {
        private readonly ILogger logger;
        private readonly IConfiguration configuration;

        public FabricBackendExtension(ILogger<FabricBackendExtension> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting...");
            logger.LogInformation(
                "Using workload name: '{WorkloadName}', PublisherTenantId: '{PublisherTenantId}', ClientId: '{ClientId}', Audience: '{Audience}'",
                WorkloadConstants.WorkloadName,
                configuration["PublisherTenantId"],
                configuration["ClientId"],
                configuration["Audience"]
                );

            //// custom Fabric extension code goes here...

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping...");
            return Task.CompletedTask;
        }
    }
}
