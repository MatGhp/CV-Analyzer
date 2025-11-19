# Document Intelligence Module
# Provides FormRecognizer service for PDF/DOCX text extraction

resource "azurerm_cognitive_account" "main" {
  name                          = "${var.name_prefix}-docintel"
  location                      = var.location
  resource_group_name           = var.resource_group_name
  kind                          = "FormRecognizer"
  sku_name                      = var.sku_name
  custom_subdomain_name         = "${var.name_prefix}-docintel"
  public_network_access_enabled = true

  # Network ACLs: Allow Container Apps and Azure services
  # Note: Container Apps Consumption plan uses dynamic outbound IPs
  # For production, consider Private Endpoints with VNet integration
  network_acls {
    default_action = "Allow" # "Deny" requires static IPs or Private Endpoint
    
    # Bypass Azure services for managed identity token exchange
    bypass = ["AzureServices"]
    
    # IP rules can be added when Container Apps uses static outbound IPs
    # ip_rules = var.allowed_ip_ranges
  }

  identity {
    type = "SystemAssigned"
  }

  lifecycle {
    create_before_destroy = true
  }

  tags = var.tags
}
