provider "azurerm" {
  version = "~> 1.35"
}

variable "default_location" {
  type    = string
  default = "Central US"
}

variable "default_resource_group" {
  type    = string
  default = "RosettaStoneV2"
}

variable "app_environment" {
  type    = string
  default = "dev"
}

variable "app_name" {
  type    = string
  default = "rosettastonev2test"
}

resource "azurerm_virtual_network" "rosettastonev2-vnet" {
  name                = "hisc-${var.app_environment}-${var.app_name}-vnet"
  resource_group_name = var.default_resource_group
  location            = var.default_location
  address_space       = ["10.6.0.0/16"]
}

resource "azurerm_subnet" "frontend-subnet" {
  name                = "hisc-${var.app_environment}-${var.app_name}-subnet-frontend"
  resource_group_name = var.default_resource_group
  virtual_network_name = "${azurerm_virtual_network.rosettastonev2-vnet.name}"
  address_prefix      = "10.6.0.0/24"
}

resource "azurerm_public_ip" "public-ip" {
  name                = "hisc-${var.app_environment}-${var.app_name}-vip"
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
    fqdns = ["hisc-dev-rosettastonev2.azurewebsites.net"]
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
