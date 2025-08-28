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

  template {
    container {
      name   = "backend-api"
      image  = var.docker_image
      cpu    = 0.5
      memory = "1.0Gi"
    }
  }
}
