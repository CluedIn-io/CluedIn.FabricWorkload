terraform {
    backend "azurerm" {
      resource_group_name  = "cluedin-terraform-rg"
      storage_account_name = "cluedinsaasterraform"
      container_name       = "fabric"
      key                  = "fabric-dev.tfstate"
  }
  required_providers {
    azurerm = {
      version = "3.17.0"
    }
    azuread = {
      version = "2.22.0"
    }
  }
}

provider "azurerm" {
  subscription_id = var.azure_subscription_id
  client_id       = var.azure_client_id
  client_secret   = var.azure_client_secret
  tenant_id       = var.azure_tenant_id
  features {}
}
