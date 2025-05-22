variable "azure_subscription_id" {
  type = string
}

variable "azure_client_id" {
  type = string
}

variable "azure_client_secret" {
  type      = string
  sensitive = true
}

variable "azure_tenant_id" {
  type = string
}

# General
variable "environment" {
  type = string
}

# Location
variable "location" {
  type = string
}

# Resource group
variable "resource_group_name" {
  type = string
}