locals {
  dns_zone_name       = "cluedin-test.online.com"    
  dns_resource_group  = "cluedin-networking"         
  record_set_name     = "fabric-ui"  
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
  depends_on = [
    azurerm_resource_group.group
  ]
  static_website {
    index_document = "index.html"
    error_404_document = "index.html"
  }
}

resource "azurerm_dns_cname_record" "fabric_ui_cname" {
  name                = local.record_set_name
  zone_name           = local.dns_zone_name
  resource_group_name = local.dns_resource_group
  ttl                 = 300
  record              = azurerm_storage_account.fabric_ui.primary_web_host
}

# Output Static Website URL
output "web_endpoint" {
  value = azurerm_storage_account.fabric_ui.primary_web_endpoint
}

# Output Custom Domain URL
output "custom_domain_url" {
  value = "https://${local.record_set_name}.${local.dns_zone_name}"
}