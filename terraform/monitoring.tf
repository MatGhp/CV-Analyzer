# Diagnostic settings for monitoring and compliance
# Uncomment and configure when Log Analytics workspace is available

# resource "azurerm_monitor_diagnostic_setting" "key_vault" {
#   name                       = "kv-diagnostics"
#   target_resource_id         = module.key_vault.key_vault_id
#   log_analytics_workspace_id = var.log_analytics_workspace_id
#
#   enabled_log {
#     category = "AuditEvent"
#   }
#
#   metric {
#     category = "AllMetrics"
#     enabled  = true
#   }
# }

# resource "azurerm_monitor_diagnostic_setting" "sql_server" {
#   name                       = "sql-diagnostics"
#   target_resource_id         = module.sql_database.sql_server_id
#   log_analytics_workspace_id = var.log_analytics_workspace_id
#
#   enabled_log {
#     category = "SQLSecurityAuditEvents"
#   }
#
#   metric {
#     category = "AllMetrics"
#     enabled  = true
#   }
# }

# TODO: Create Log Analytics workspace and uncomment above resources
