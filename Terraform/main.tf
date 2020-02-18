provider "azuread" {
  version = "~> 0.6"
}

provider "azurerm" {
  version = "~> 1.35"
}

provider "external" {
  version = "~> 1.2"
}

variable "default_location" {
  type = string
}

variable "default_resource_group" {
  type = string
}

variable "app_environment" {
  type = string
}

variable "app_name" {
  type = string
}

variable "clearcare_client_id" {
  type = string
}

variable "clearcare_client_secret" {
  type = string
}

variable "clearcare_username" {
  type = string
}

variable "clearcare_password" {
  type = string
}

variable "cache_expiration_in_sec" {
  type = number
}

variable "app_root" {
  type    = string
  default = "RosettaStoneV2"
}

variable "aspnet_environment" {
  type    = string
}

variable "retention_in_days" {
  type    = number
  default = 7
}

variable "retention_in_mb" {
  type    = number
  default = 35
}

data "azurerm_client_config" "current" {}

## the following block is a workaround pulled from https://github.com/terraform-providers/terraform-provider-azurerm/issues/3502
data "external" "this_az_account" {
  program = [
    "az",
    "ad",
    "signed-in-user",
    "show",
    "--query",
    "{displayName: displayName,objectId: objectId,objectType: objectType,odata_metadata: \"odata.metadata\"}"
  ]
}

# create the resource groups RosettaStoneV2
resource "azurerm_resource_group" "rosettastone-rg" {
  name     = var.app_root
  location = var.default_location
  tags     = {
    "Project"             = "Integrated Lead Management"
    "Target"              = "Home Office"
    "App Name"            = var.app_root
    "Assigned Department" = "IT Services"
    "Assigned Company"    = "Home Office"
	"Environment" = "Production"
  }
}

# create a linux app service plan for rosettastone
resource "azurerm_app_service_plan" "linux-rosettastone-asp" {
  name                = "hisc-${var.app_environment}-${var.app_name}-plan"
  location            = var.default_location
  resource_group_name = var.app_root

  # Define Linux as Host OS
  kind = "Linux"

  # Choose size
  sku {
    tier = "Standard"
    size = "S1"
  }

  reserved = true # Mandatory for Linux plans
}

resource "azuread_application" "rosettastone_app" {
  name                       = "hisc-${var.app_environment}-${var.app_name}-app"
  homepage                   = "https://hisc-${var.app_environment}-${var.app_name}-app.azurewebsites.net/"
  identifier_uris            = ["https://hisc-${var.app_environment}-${var.app_name}-app.azurewebsites.net"]
  reply_urls                 = ["https://hisc-${var.app_environment}-${var.app_name}-app.azurewebsites.net/.auth/login/aad/callback"]
  available_to_other_tenants = false
  oauth2_allow_implicit_flow = true

  required_resource_access {
    # Azure Active Directory Graph
    resource_app_id = "00000002-0000-0000-c000-000000000000"

    # User.Read
    resource_access {
      id   = "311a71cc-e848-46a1-bdf8-97ff7156d8e6"
      type = "Scope"
    }
  }
}

resource "azuread_service_principal" "rosettastone_sp" {
  application_id               = "${azuread_application.rosettastone_app.application_id}"
  app_role_assignment_required = false
}

# create an app service for the rosettastone service
resource "azurerm_app_service" "rosettastone-as" {
  name                = "hisc-${var.app_environment}-${var.app_name}-as" #this has to be unique across all subscriptions, used for the hostname
  location            = var.default_location
  resource_group_name = var.app_root
  app_service_plan_id = "${azurerm_app_service_plan.linux-rosettastone-asp.id}"

  identity {
    type = "SystemAssigned"
  }

  # require https
  https_only = true

  site_config {
    always_on = true
  }

  # see logs in azure
  logs {
    http_logs {
      file_system {
        retention_in_days = var.retention_in_days
        retention_in_mb   = var.retention_in_mb
      }
    }
  }

  app_settings = {
    WEBSITES_ENABLE_APP_SERVICE_STORAGE = false
    ASPNETCORE_ENVIRONMENT              = "${var.aspnet_environment}"
    APPLICATION_AI_KEY                  = "${azurerm_application_insights.rosettastone-ai.instrumentation_key}"
    APPLICATION_KEYVAULTURL             = "https://hisc-${var.app_environment}-${var.app_name}-kv.vault.azure.net/secrets/"
    WEBSITE_HTTPLOGGING_RETENTION_DAYS  = var.retention_in_days
  }
}

# create the key vault
resource "azurerm_key_vault" "rosettastone-kv" {
  name                            = "hisc-${var.app_environment}-${var.app_name}-kv" #this has to be unique across all subscriptions and between 3-24 characters
  location                        = var.default_location
  resource_group_name             = var.app_root
  sku_name                        = "standard"
  tenant_id                       = "${data.azurerm_client_config.current.tenant_id}"
  enabled_for_deployment          = false
  enabled_for_disk_encryption     = false
  enabled_for_template_deployment = false

  access_policy {
    tenant_id       = "${data.azurerm_client_config.current.tenant_id}"
    object_id       = "${data.external.this_az_account.result.objectId}"
    key_permissions = []
    secret_permissions = [
      "Get",
      "List",
      "Set",
      "Delete",
    ]
    certificate_permissions = []
  }

  access_policy {
    tenant_id       = "${data.azurerm_client_config.current.tenant_id}"
    object_id       = "${azurerm_app_service.rosettastone-as.identity[0].principal_id}"
    key_permissions = []
    secret_permissions = [
      "Get",
      "List",
    ]
    certificate_permissions = []
  }
}

resource "azurerm_key_vault_secret" "ClearCareClientId" {
  name         = "ClearCareClientId"
  value        = "${var.clearcare_client_id}"
  key_vault_id = "${azurerm_key_vault.rosettastone-kv.id}"
}

resource "azurerm_key_vault_secret" "ClearCareClientSecret" {
  name         = "ClearCareClientSecret"
  value        = "${var.clearcare_client_secret}"
  key_vault_id = "${azurerm_key_vault.rosettastone-kv.id}"
}

resource "azurerm_key_vault_secret" "ClearCareUsername" {
  name         = "ClearCareUsername"
  value        = "${var.clearcare_username}"
  key_vault_id = "${azurerm_key_vault.rosettastone-kv.id}"
}

resource "azurerm_key_vault_secret" "ClearCarePassword" {
  name         = "ClearCarePassword"
  value        = "${var.clearcare_password}"
  key_vault_id = "${azurerm_key_vault.rosettastone-kv.id}"
}

resource "azurerm_key_vault_secret" "CacheExpirationInSec" {
  name         = "CacheExpirationInSec"
  value        = "${var.cache_expiration_in_sec}"
  key_vault_id = "${azurerm_key_vault.rosettastone-kv.id}"
}

resource "azurerm_key_vault_secret" "ManuallyMappedFranchisesJson" {
  name         = "ManuallyMappedFranchisesJson"
  value        = <<JSON
[{"franchise_number":"244","clear_care_agency":3465}]
JSON
  key_vault_id = "${azurerm_key_vault.rosettastone-kv.id}"
}

# create the application insight for rosettastone
resource "azurerm_application_insights" "rosettastone-ai" {
  name                = "hisc-${var.app_environment}-${var.app_name}-ai"
  location            = var.default_location
  resource_group_name = var.app_root
  application_type    = "web"
}

## create the storage account required to save local information to blob
resource "azurerm_storage_account" "rosettastone-storage" {
  name                     = "hisc${var.app_environment}${var.app_name}storage"
  resource_group_name      = azurerm_resource_group.rosettastone-rg.name
  location                 = azurerm_resource_group.rosettastone-rg.location
  account_kind             = "StorageV2"
  account_tier             = "Standard"
  account_replication_type = "LRS"
}
