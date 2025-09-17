locals {
  dns_zone_name       = "cluedin-test.online.com"    
  dns_resource_group  = "cluedin-networking"         
  record_set_name     = "fabric-ui"  
}

# Resource group
resource "azurerm_resource_group" "group" {
  name     = var.resource_group_name
  location = var.location
  tags     = var.tags
}

data "azurerm_container_registry" "acr" {
  name                = "cluedindev"
  resource_group_name = "oversight-rg"
  provider            = azurerm.cluedin_develop
}

resource "azurerm_user_assigned_identity" "frontend_identity" {
  name                = "identity-cluedin-frontendweu-dev"
  resource_group_name = azurerm_resource_group.group.name
  location            = azurerm_resource_group.group.location
  tags                = var.tags
}

resource "azurerm_role_assignment" "frontend_acr_pull" {
  scope                = data.azurerm_container_registry.acr.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.frontend_identity.principal_id
}

resource "azurerm_container_app_environment" "app_env" {
  name                = "env-cluedin-frontend-weu-dev"
  location            = azurerm_resource_group.group.location
  resource_group_name = azurerm_resource_group.group.name
  tags                = var.tags
}

resource "azurerm_container_app" "frontend" {
  name                         = "ui-cluedin-frontend-weu-dev"
  resource_group_name          = azurerm_resource_group.group.name
  container_app_environment_id = azurerm_container_app_environment.app_env.id
  revision_mode                = "Single"
  tags                         = var.tags

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.frontend_identity.id]
  }

  ingress {
    external_enabled = true
    target_port      = 80

    traffic_weight {
      percentage       = 100
      latest_revision  = true
    }
  }

  registry {
    server   = data.azurerm_container_registry.acr.login_server
    identity = azurerm_user_assigned_identity.frontend_identity.id
  }

  template {
    container {
      name   = "frontend-ui"
      image  = "${var.docker_image}:${var.image_tag}"
      cpu    = 0.5
      memory = "1.0Gi"
    }
  }

  depends_on = [azurerm_role_assignment.frontend_acr_pull]
}

data "azurerm_dns_zone" "main" {
  name                = "cluedin-test.online"
  resource_group_name = "cluedin-networking"
  provider            = azurerm.cluedin_develop
}

# CNAME to Container App
resource "azurerm_dns_cname_record" "fabric_ui_cname" {
  provider            = azurerm.cluedin_develop
  name                = local.record_set_name
  zone_name           = data.azurerm_dns_zone.main.name
  resource_group_name = data.azurerm_dns_zone.main.resource_group_name
  ttl                 = 300
  record              = azurerm_container_app.frontend.latest_revision_fqdn


}

# TXT record for domain verification
resource "azurerm_dns_txt_record" "frontend_verification" {
  provider            = azurerm.cluedin_develop
  name                = "asuid.${local.record_set_name}"
  zone_name           = data.azurerm_dns_zone.main.name
  resource_group_name = data.azurerm_dns_zone.main.resource_group_name
  ttl                 = 300

  record {
    value = azurerm_container_app.frontend.custom_domain_verification_id
  }
}

# Managed Certificate
resource "azapi_resource" "frontend_cert" {
  type      = "Microsoft.App/managedEnvironments/managedCertificates@2023-05-01"
  name      = "fabric-ui-cert"
  parent_id = azurerm_container_app_environment.app_env.id
  location  = azurerm_container_app_environment.app_env.location
  schema_validation_enabled = false

  body = jsonencode({
    properties = {
      subjectName = "fabric-ui.cluedin-test.online"

    }
  })
}

# Bind custom domain + cert
resource "azapi_resource" "frontend_domain_binding" {
  type      = "Microsoft.App/containerApps/customDomains@2023-05-01"
  name      = "fabric-ui"
  parent_id = azurerm_container_app.frontend.id

  body = jsonencode({
    properties = {
      hostname      = "fabric-ui.cluedin-test.online"
      certificateId = azapi_resource.frontend_cert.id
    }
  })

  depends_on = [
    azurerm_dns_cname_record.fabric_ui_cname,
    azurerm_dns_txt_record.frontend_verification,
    azapi_resource.frontend_cert,
  ]
}


