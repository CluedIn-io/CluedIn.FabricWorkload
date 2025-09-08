locals {
  dns_zone_name       = "cluedin-test.online.com"    
  dns_resource_group  = "cluedin-networking"         
  record_set_name     = "fabric-ui"  
}

# Resource group
resource "azurerm_resource_group" "group" {
  name     = var.resource_group_name
  location = var.location
  tags = var.tags
}

resource "azurerm_storage_account" "fabric_ui" {
  name                     = "stcluedinfabricweudev"
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  tags = var.tags
  depends_on = [
    azurerm_resource_group.group
  ]
  static_website {
    index_document = "index.html"
    error_404_document = "index.html"
  }
}

resource "azurerm_cdn_profile" "ui_cdn_profile" {
  name                = "cluedinfabricui-cdn-profile"
  location            = "global"
  resource_group_name = azurerm_resource_group.group.name
  sku                 = "Standard_Microsoft" # Required for HTTPS
  tags                = var.tags
}

# -------------------------
# CDN Endpoint (origin = storage static website)
# -------------------------
resource "azurerm_cdn_endpoint" "ui_cdn_endpoint" {
  name                = "cluedinfabricui-cdn-endpoint"
  profile_name        = azurerm_cdn_profile.ui_cdn_profile.name
  resource_group_name = azurerm_resource_group.group.name
  location            = "global"
  origin_host_header  = azurerm_storage_account.fabric_ui.primary_web_host

  origin {
    name      = "fabricuistorage"
    host_name = azurerm_storage_account.fabric_ui.primary_web_host
  }

  is_http_allowed  = true
  is_https_allowed = true
  tags             = var.tags
}

resource "azurerm_cdn_custom_domain" "ui_cdn_custom_domain" {
  name                   = "fabric-ui-custom-domain"
  profile_name           = azurerm_cdn_profile.ui_cdn_profile.name
  endpoint_name          = azurerm_cdn_endpoint.ui_cdn_endpoint.name
  resource_group_name    = azurerm_resource_group.group.name
  host_name              = "${local.record_set_name}.${local.dns_zone_name}"

  # Free managed HTTPS certificate
  custom_https_provisioning_enabled = true
}

data "azurerm_dns_zone" "main" {
  name                = "cluedin-test.online"
  resource_group_name = "cluedin-networking"
  provider = azurerm.cluedin_develop
}


resource "azurerm_dns_cname_record" "fabric_ui_cname" {
  provider            = azurerm.cluedin_develop
  name                = "fabric-ui"
  zone_name           = data.azurerm_dns_zone.main.name
  resource_group_name = data.azurerm_dns_zone.main.resource_group_name
  ttl                 = 300
  record              = azurerm_storage_account.fabric_ui.primary_web_host
}


# Output Static Website URL
output "web_endpoint" {
  value = azurerm_storage_account.fabric_ui.primary_web_endpoint
}

output "cdn_endpoint" {
  description = "CDN endpoint URL"
  value       = "https://${azurerm_cdn_endpoint.ui_cdn_endpoint.host_name}"
}

# Output Custom Domain URL
output "custom_domain_url" {
  value = "https://${local.record_set_name}.${local.dns_zone_name}"
}