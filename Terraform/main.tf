
provider "azuread" {
  version = "~> 0.6"
}

provider "azurerm" {
  version = "~> 1.35"
}

provider "external" {
  version = "~> 1.2"
}

variable "app_prefix" {
  type    = string
  default = "hisc"
}

variable "app_root" {
  type    = string
  default = "RosettaStoneV2"
}

variable "app_root_lower" {
  type    = string
  default = "rosettastonev2"
}

variable "app_environment_identifier" {
  type    = string
  default = "qa"
}

variable "aspnet_environment" {
  type    = string
  default = "Staging"
}

variable "default_location" {
  type    = string
  default = "Central US"
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
resource azurerm_resource_group rosettastone-rg {
  name     = var.app_root
  location = var.default_location
  tags = {
    "Project"             = "Integrated Lead Management"
    "Target"              = "Home Office"
    "App Name"            = var.app_root
    "Assigned Department" = "IT Services"
    "Assigned Company"    = "Home Office"
  }
}


# create a linux app service plan for rosettastone
resource azurerm_app_service_plan linux-rosettastone-asp {
  name                = "${var.app_prefix}-${var.app_environment_identifier}-${var.app_root_lower}-plan"
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


resource azuread_application rosettastone_app {
  name                       = "${var.app_prefix}-${var.app_environment_identifier}-${var.app_root_lower}-app"
  homepage                   = "https://${var.app_prefix}-${var.app_environment_identifier}-${var.app_root_lower}.azurewebsites.net/"
  identifier_uris            = ["https://${var.app_prefix}-${var.app_environment_identifier}-${var.app_root_lower}.azurewebsites.net"]
  reply_urls                 = ["https://${var.app_prefix}-${var.app_environment_identifier}-${var.app_root_lower}.azurewebsites.net/.auth/login/aad/callback"]
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

resource azuread_service_principal rosettastone_sp {
  application_id               = "${azuread_application.rosettastone_app.application_id}"
  app_role_assignment_required = false
}

# create an app service for the rosettastone service
resource azurerm_app_service rosettastone-as {
  name                = "${var.app_prefix}-${var.app_environment_identifier}-${var.app_root_lower}" #this has to be unique across all subscriptions, used for the hostname
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
    APPLICATION_KEYVAULTURL             = "https://${var.app_prefix}-${var.app_environment_identifier}-${var.app_root_lower}.vault.azure.net/secrets/"
    WEBSITE_HTTPLOGGING_RETENTION_DAYS  = var.retention_in_days
  }
}


# create the key vault
resource azurerm_key_vault rosettastone-kv {
  name                            = "${var.app_prefix}-${var.app_environment_identifier}-${var.app_root_lower}" #this has to be unique across all subscriptions
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

resource azurerm_key_vault_secret "ClearCareClientId" {
  name         = "ClearCareClientId"
  value        = "replace_me_once_created"
  key_vault_id = "${azurerm_key_vault.rosettastone-kv.id}"
}
resource azurerm_key_vault_secret "ClearCareClientSecret" {
  name         = "ClearCareClientSecret"
  value        = "replace_me_once_created"
  key_vault_id = "${azurerm_key_vault.rosettastone-kv.id}"
}

resource azurerm_key_vault_secret "ClearCareUsername" {
  name         = "ClearCareUsername"
  value        = "replace_me_once_created"
  key_vault_id = "${azurerm_key_vault.rosettastone-kv.id}"
}

resource azurerm_key_vault_secret "ClearCarePassword" {
  name         = "ClearCarePassword"
  value        = "replace_me_once_created"
  key_vault_id = "${azurerm_key_vault.rosettastone-kv.id}"
}
resource azurerm_key_vault_secret "CacheExpirationInSec" {
  name         = "CacheExpirationInSec"
  value        = "replace_me_once_created"
  key_vault_id = "${azurerm_key_vault.rosettastone-kv.id}"
}

# create the application insight for rosettastone
resource azurerm_application_insights rosettastone-ai {
  name                = "${var.app_prefix}-${var.app_environment_identifier}-${var.app_root_lower}-ai"
  location            = var.default_location
  resource_group_name = var.app_root
  application_type    = "web"
}


