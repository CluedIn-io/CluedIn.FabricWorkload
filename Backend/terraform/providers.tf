terraform {
    backend "azurerm" {
      resource_group_name  = "cluedin-terraform-rg"
      storage_account_name = "cluedinsaasterraform"
      container_name       = "fabricbackend"
      key                  = "fabric-dev.tfstate"
  }
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">=3.75.0"
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

provider "azurerm" {
  subscription_id = var.azure_subscription_id_develop
  client_id       = var.azure_client_id
  client_secret   = var.azure_client_secret
  tenant_id       = var.azure_tenant_id
  features {}
  alias = "cluedin_develop"
}