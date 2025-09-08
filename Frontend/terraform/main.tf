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

resource "azurerm_cdn_frontdoor_profile" "ui" {
  name                = "cluedinfabricui-fd-profile"
  resource_group_name = azurerm_resource_group.group.name
  sku_name            = "Standard_AzureFrontDoor"
  tags                = var.tags
}

resource "azurerm_cdn_frontdoor_endpoint" "ui" {
  name                     = "cluedinfabricui-fd-endpoint"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.ui.id
  enabled                  = true
}

resource "azurerm_cdn_frontdoor_origin_group" "ui" {
  name                     = "fabricui-origins"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.ui.id
  session_affinity_enabled = false

  load_balancing {
    sample_size                        = 4
    successful_samples_required        = 3
    additional_latency_in_milliseconds = 0
  }

  health_probe {
    interval_in_seconds = 120
    path                = "/"
    protocol            = "Https"
    request_type        = "GET"
  }
}

resource "azurerm_cdn_frontdoor_origin" "ui_storage" {
  name                          = "fabricuistorage"
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.ui.id
  enabled                       = true
  host_name                     = azurerm_storage_account.fabric_ui.primary_web_host
  certificate_name_check_enabled = true

}
resource "azurerm_cdn_frontdoor_route" "ui" {
  name                          = "fabricui-route"
  cdn_frontdoor_endpoint_id     = azurerm_cdn_frontdoor_endpoint.ui.id
  cdn_frontdoor_origin_group_id = azurerm_cdn_frontdoor_origin_group.ui.id
  cdn_frontdoor_origin_ids      = [azurerm_cdn_frontdoor_origin.ui_storage.id]

  supported_protocols  = ["Http", "Https"]
  patterns_to_match    = ["/*"]
  https_redirect_enabled = true
  enabled              = true
  cdn_frontdoor_custom_domain_ids = [azurerm_cdn_frontdoor_custom_domain.ui.id]
}

# Custom Domain
resource "azurerm_cdn_frontdoor_custom_domain" "ui" {
  name                     = "fabric-ui-domain"
  cdn_frontdoor_profile_id = azurerm_cdn_frontdoor_profile.ui.id
  host_name                = "${local.record_set_name}.${local.dns_zone_name}"

  tls {
    certificate_type    = "ManagedCertificate"
    minimum_tls_version = "TLS12"
  }
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
  #record              = azurerm_storage_account.fabric_ui.primary_web_host
  record              = azurerm_cdn_frontdoor_endpoint.ui.host_name
}


# Output Static Website URL
output "web_endpoint" {
  value = azurerm_storage_account.fabric_ui.primary_web_endpoint
}

output "cdn_endpoint" {
  description = "Front Door endpoint URL"
  value       = "https://${azurerm_cdn_frontdoor_endpoint.ui.host_name}"
}

# Output Custom Domain URL
output "custom_domain_url" {
  value = "https://${local.record_set_name}.${local.dns_zone_name}"
}