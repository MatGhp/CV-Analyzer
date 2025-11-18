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

# SQL Admin Password (if not using environment variable)
# sql_admin_password = "YourStrongPassword123!"
