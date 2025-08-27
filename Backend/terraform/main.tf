locals {
  app_package_md5    = filemd5(var.function_app_package)
}

# Resource group
resource "azurerm_resource_group" "group" {
  name     = var.resource_group
  location = var.location
}

# Storage account
resource "azurerm_storage_account" "store" {
  name                     = var.storage_account_name
  resource_group_name      = azurerm_resource_group.group.name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "RAGRS"
}

# App Insights / Workspace
resource "azurerm_log_analytics_workspace" "workspace" {
  name                = "la-${var.function_app_name}"
  location            = azurerm_resource_group.group.location
  resource_group_name = azurerm_resource_group.group.name
  retention_in_days   = 30
}

resource "azurerm_application_insights" "insights" {
  name                = "in-${var.function_app_name}"
  location            = azurerm_resource_group.group.location
  resource_group_name = azurerm_resource_group.group.name
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.workspace.id
}

# App plan
resource "azurerm_service_plan" "plan" {
  name                = var.app_service_plan
  resource_group_name = azurerm_resource_group.group.name
  location            = azurerm_resource_group.group.location
  os_type             = "Linux"
  sku_name            = "S1"
}

resource "azurerm_linux_function_app" "webhook" {
  name                = var.function_app_name
  resource_group_name = azurerm_resource_group.group.name
  location            = azurerm_resource_group.group.location

  storage_account_name       = azurerm_storage_account.store.name
  storage_account_access_key = azurerm_storage_account.store.primary_access_key

  service_plan_id = azurerm_service_plan.plan.id

  site_config {
    application_insights_connection_string    = azurerm_application_insights.insights.connection_string
    application_insights_key                  = azurerm_application_insights.insights.instrumentation_key
    always_on                              = true

    application_stack {
      dotnet_version              = "8.0"
      use_dotnet_isolated_runtime = true
    }
  }

  zip_deploy_file             = var.function_app_package
  
  app_settings = {
    WEBSITE_RUN_FROM_PACKAGE                  = 1
    QueueEmailRequests                        = var.queue_name_email_requests
    QueueLifeCycleEvents                      = var.queue_name_life_cycle_events
    QueueLifeCycleActions                     = var.queue_name_life_cycle_actions
    QueueInfrastructureAlertActions           = var.queue_name_infrastructure_alerts
    EmailerBaseUrl                            = var.emailer_base_url
    EmailerApiKey                             = var.emailer_api_key
    BillingBaseUrl                            = var.billingBaseUrl
    BillingApiKey                             = var.billingApiKey
    AutomationWebhookUrl                      = var.automationWebhookUrl
    LifeCycleWebhookUnsubscribe               = var.lifecycle_webhook_unsubscribe
    LifeCycleWebhookRemove                    = var.lifecycle_webhook_remove
    LifeCycleWebhookScale                     = var.lifecycle_webhook_scale
    SlackUrl                                  = var.slackUrl
    DefaultReleaseVersion                     = var.defaultReleaseVersion
    ENVIRONMENT                               = var.environment
  }     
}