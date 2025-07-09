locals {
}

# Resource group
resource "azurerm_resource_group" "group" {
  name     = var.resource_group_name
  location = var.location
}

resource "azurerm_storage_account" "fabric_ui" {
  name                     = "stcluedinfabricweudev"
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  static_website {
    index_document = "index.html"
    error_404_document = "index.html"
  }
}

output "web_endpoint" {
  value = azurerm_storage_account.fabric_ui.primary_web_endpoint
}