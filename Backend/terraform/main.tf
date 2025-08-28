data "azurerm_container_registry" "acr" {
  name                = "cluedindev"
  resource_group_name = "oversight-rg"
  provider = azurerm.cluedin_develop
}

resource "azurerm_container_app_environment" "app_env" {
  name                = "backend-env"
  location            = var.location
  resource_group_name = var.resource_group_name
}

resource "azurerm_container_app" "backend" {
  name                         = "backend-api"
  resource_group_name          = var.resource_group_name
  container_app_environment_id = azurerm_container_app_environment.app_env.id
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  template {
    container {
      name   = "backend-api"
      image  = var.docker_image
      cpu    = 0.5
      memory = "1.0Gi"
    }
  }
}

resource "azurerm_role_assignment" "backend_acr_pull" {
  scope                            = data.azurerm_container_registry.acr.id
  role_definition_name             = "AcrPull"
  principal_id                     = azurerm_container_app.backend.identity[0].principal_id
  skip_service_principal_aad_check = true
}