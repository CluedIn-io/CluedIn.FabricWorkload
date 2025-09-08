variable "resource_group_name" {
  default = "rg-cluedin-fabric-weu-dev-backend"
}

variable "location" {
  default = "westeurope"
}

resource "azurerm_resource_group" "group" {
  name     = var.resource_group_name
  location = var.location
  tags = var.tags
}


data "azurerm_container_registry" "acr" {
  name                = "cluedindev"
  resource_group_name = "oversight-rg"
  provider = azurerm.cluedin_develop
}

resource "azurerm_user_assigned_identity" "backend_identity" {
  name                = "identity-cluedin-fabric-backend-west-eu-dev"
  resource_group_name = azurerm_resource_group.group.name
  location            = azurerm_resource_group.group.location
  tags = var.tags
}


resource "azurerm_role_assignment" "backend_acr_pull" {
  scope                = data.azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.backend_identity.principal_id
}


resource "azurerm_container_app_environment" "app_env" {
  name                = "env-cluedin-fabric-backend-west-eu-dev"
  location            = azurerm_resource_group.group.location
  resource_group_name = azurerm_resource_group.group.name
  tags = var.tags
}

resource "azurerm_storage_account" "backend_storage" {
  name                     = "stcluedinbackendweudev"
  resource_group_name      = azurerm_resource_group.group.name
  location                 = azurerm_resource_group.group.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  tags = var.tags
}

resource "random_string" "unique" {
  length  = 6
  upper   = false
  lower   = true
  numeric = true
  special = false
}

# Storage connection string
output "storage_connection_string" {
  value     = azurerm_storage_account.backend_storage.primary_connection_string
  sensitive = true
}


resource "azurerm_container_app" "backend" {
  name                         = "api-cluedin-fabric-backend-west-eu-dev"
  resource_group_name          = azurerm_resource_group.group.name
  container_app_environment_id = azurerm_container_app_environment.app_env.id
  revision_mode                = "Single"
  tags = var.tags

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.backend_identity.id]
  }
  ingress {
    external_enabled = true
    target_port      = 5000
    traffic_weight {
      percentage = 100
      latest_revision = true
    }
    custom_domain {
      name = "fabric-api.cluedin-test.online"
    }
  }
  registry {
    server   = data.azurerm_container_registry.acr.login_server
    identity = azurerm_user_assigned_identity.backend_identity.id
  }
  template {
    container {
      name   = "backend-api"
      image  = "${var.docker_image}:${var.image_tag}"
      cpu    = 0.5
      memory = "1.0Gi"
      env {
        name  = "TableStorageConnectionString"
        value = "DefaultEndpointsProtocol=https;AccountName=${azurerm_storage_account.backend_storage.name};AccountKey=${azurerm_storage_account.backend_storage.primary_access_key};EndpointSuffix=core.windows.net"

      }
      env {
        name  = "clientId"
        value = var.azure_client_id
      }

      env {
        name  = "clientSecret"
        value = var.azure_client_secret
    }
      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Development"
      }
      env {
        name  = "ItemMetadataStoreType"
        value = "TableStorage"
      }

    }
    
  }

  depends_on = [
    azurerm_role_assignment.backend_acr_pull
  ]
}

data "azurerm_dns_zone" "main" {
  name                = "cluedin-test.online"
  resource_group_name = "cluedin-networking"
  provider = azurerm.cluedin_develop
}

resource "azurerm_dns_cname_record" "fabric_api_dns" {
  provider            = azurerm.cluedin_develop
  name                = "fabric-api"
  zone_name           = data.azurerm_dns_zone.main.name
  resource_group_name = data.azurerm_dns_zone.main.resource_group_name
  ttl                 = 300
  record              = azurerm_container_app.backend.ingress[0].fqdn
}
