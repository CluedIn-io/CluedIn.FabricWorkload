// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Boilerplate.Contracts;
using Boilerplate.Items;
using Fabric_Extension_BE_Boilerplate.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace Boilerplate.Services
{
    public class ItemFactory : IItemFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IItemMetadataStore _itemMetadataStore;
        private readonly ILakehouseClientService _lakeHouseClientService;
        private readonly IAuthenticationService _authenticationService;

        public ItemFactory(
            IServiceProvider serviceProvider,
            IItemMetadataStore itemMetadataStore,
            ILakehouseClientService lakeHouseClientService,
            IAuthenticationService authenticationService)
        {
            _serviceProvider = serviceProvider;
            _itemMetadataStore = itemMetadataStore;
            _lakeHouseClientService = lakeHouseClientService;
            _authenticationService = authenticationService;
        }

        public IItem CreateItem(string itemType, AuthorizationContext authorizationContext)
        {
            if (itemType == WorkloadConstants.ItemTypes.Item1)
            {
                return new Item1(_serviceProvider.GetService<ILogger<Item1>>(), _itemMetadataStore, _lakeHouseClientService, _authenticationService, authorizationContext);
            }

            if (itemType == WorkloadConstants.ItemTypes.CleanProjectItem)
            {
                return new CleanProjectItem(
                    _serviceProvider.GetService<ILogger<CleanProjectItem>>(),
                    _serviceProvider.GetService<ILoggerFactory>(),
                    _itemMetadataStore,
                    _serviceProvider.GetService<IHttpClientFactory>(),
                    _lakeHouseClientService,
                    _authenticationService,
                    authorizationContext);
            }
            throw new NotSupportedException($"Items of type {itemType} are not supported");
        }
    }
}
