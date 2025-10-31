# Resource locks to prevent accidental deletion in production
# These locks must be manually removed before destroying resources

resource "azurerm_management_lock" "resource_group_lock" {
  count      = var.environment == "prod" ? 1 : 0
  name       = "prevent-delete-lock"
  scope      = azurerm_resource_group.main.id
  lock_level = "CanNotDelete"
  notes      = "Prevents accidental deletion of production resources"
}
