# Document Intelligence Module
# Provides FormRecognizer service for PDF/DOCX text extraction

resource "azurerm_cognitive_account" "main" {
  name                = "${var.name_prefix}-docintel"
  location            = var.location
  resource_group_name = var.resource_group_name
  kind                = "FormRecognizer"
  sku_name            = var.sku_name

  identity {
    type = "SystemAssigned"
  }

  lifecycle {
    create_before_destroy = true
  }

  tags = var.tags
}
