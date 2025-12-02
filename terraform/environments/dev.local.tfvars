# Local Development Configuration Example
# Copy this file to dev.local.tfvars and add your actual values
# dev.local.tfvars is gitignored and safe for sensitive local settings

# SQL Firewall Rules - Add your development machine IP
sql_firewall_rules = {
  "AllowDevMachine" = {
    start_ip_address = "YOUR_IP_HERE" # e.g., "203.0.113.42"
    end_ip_address   = "YOUR_IP_HERE"
  }
}

# SQL Admin Password
# RECOMMENDED: Set via environment variable instead of this file:
#   PowerShell: $env:TF_VAR_sql_admin_password = "YourPassword"
#   Bash: export TF_VAR_sql_admin_password="YourPassword"
# Use same password as GitHub secret SQL_ADMIN_PASSWORD for consistency
# sql_admin_password = "<YOUR_SQL_PASSWORD>"
