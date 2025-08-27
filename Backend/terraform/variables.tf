# General
variable "environment" {
  type = string
}

# Resource group
variable "resource_group" {
  type = string
}

# Location
variable "location" {
  type = string
}

# The following are required for the azurerm provider
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

variable "app_service_plan" {
  type = string
}

variable "function_app_name" {
  type = string
}

variable "function_app_package" {
  type = string
}

variable "storage_account_name" {
  type = string
}

variable "container_name" {
  type = string
}

variable "emailer_base_url" {
  type = string
}

variable "emailer_api_key" {
  type = string
}

variable "service_bus_name" {
  type = string
}

variable "queue_name_email_requests" {
  type = string
}

variable "queue_name_life_cycle_events" {
  type = string
}

variable "queue_name_life_cycle_actions" {
  type = string
}

variable "queue_name_infrastructure_alerts" {
  type = string
}

variable "billingBaseUrl" {
  type = string
}

variable "billingApiKey" {
  type = string
}

variable "automationWebhookUrl" {
  type = string
}

variable "lifecycle_webhook_unsubscribe" {
  type = string
}

variable "lifecycle_webhook_remove" {
  type = string
}

variable "lifecycle_webhook_scale" {
  type = string
}

variable "slackUrl" {
  type = string
}

variable "defaultReleaseVersion" {
  type = string
}