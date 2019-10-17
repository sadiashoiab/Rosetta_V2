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

# create the application insight for rosettastone
resource "azurerm_application_insights" "rosettastone-ai" {
  name                = "hisc-${var.app_environment}-${var.app_name}-ai"
  location            = var.default_location
  resource_group_name = var.app_root
  application_type    = "web"
}

resource "azurerm_virtual_network" "rosettastonev2-vnet" {
  name                = "hisc-${var.app_environment}-${var.app_name}-vnet"
  resource_group_name = var.default_resource_group
  location            = var.default_location
  address_space       = ["10.6.0.0/16"]
}

resource "azurerm_subnet" "frontend-subnet" {
  name                = "hisc-${var.app_environment}-${var.app_name}-subnet-fe"
  resource_group_name = var.default_resource_group
  virtual_network_name = "${azurerm_virtual_network.rosettastonev2-vnet.name}"
  address_prefix      = "10.6.0.0/24"
}

resource "azurerm_public_ip" "public-ip" {
  name                = "hisc-${var.app_environment}-${var.app_name}-pip"
  resource_group_name = var.default_resource_group
  location            = var.default_location
  allocation_method   = "Static"
  sku                 = "Standard"
}

resource "azurerm_application_gateway" "rosettastonev2-ag" {
  name                = "hisc-${var.app_environment}-${var.app_name}-ag"
  resource_group_name = var.default_resource_group
  location            = var.default_location

  sku {
    name     = "Standard_v2"
    tier     = "Standard_v2"
    capacity = 1
  }

  gateway_ip_configuration {
    name      = "ag-ip-configuration" 
    subnet_id = "${azurerm_subnet.frontend-subnet.id}"
  }

  frontend_port {
    name = "${azurerm_virtual_network.rosettastonev2-vnet.name}-fe-port"
    port = 4567
  }

  frontend_ip_configuration {
    name                 = "${azurerm_virtual_network.rosettastonev2-vnet.name}-fe-ip"
    public_ip_address_id = "${azurerm_public_ip.public-ip.id}"
  }

  backend_address_pool {
    name  = "${azurerm_virtual_network.rosettastonev2-vnet.name}-be-ap"
    fqdns = ["${azuread_application.rosettastone-as.name}.azurewebsites.net"]
  }

  backend_http_settings {
    name                                = "${azurerm_virtual_network.rosettastonev2-vnet.name}-be-hs"
    cookie_based_affinity               = "Disabled"
    protocol                            = "Https"
    port                                = 443
    request_timeout                     = 90
    pick_host_name_from_backend_address = true
    probe_name                          = "${azurerm_virtual_network.rosettastonev2-vnet.name}-probe"
  }

  http_listener {
    name                           = "${azurerm_virtual_network.rosettastonev2-vnet.name}-http-lstn"
    frontend_ip_configuration_name = "${azurerm_virtual_network.rosettastonev2-vnet.name}-fe-ip"
    frontend_port_name             = "${azurerm_virtual_network.rosettastonev2-vnet.name}-fe-port"
    protocol                       = "Http" # this should be https but we need a cert first
  }

  request_routing_rule {
    name                       = "${azurerm_virtual_network.rosettastonev2-vnet.name}-rq-rt"
    rule_type                  = "Basic"
    http_listener_name         = "${azurerm_virtual_network.rosettastonev2-vnet.name}-http-lstn"
    backend_address_pool_name  = "${azurerm_virtual_network.rosettastonev2-vnet.name}-be-ap"
    backend_http_settings_name = "${azurerm_virtual_network.rosettastonev2-vnet.name}-be-hs"
  }

  probe {
    name                                      = "${azurerm_virtual_network.rosettastonev2-vnet.name}-probe"
    protocol                                  = "Https"
    pick_host_name_from_backend_http_settings = true
    path                                      = "/health"
    interval                                  = 30
    timeout                                   = 30
    unhealthy_threshold                       = 3
  }
}
