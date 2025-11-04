# Test Environment Configuration

environment = "test"
location    = "germanywestcentral"

# SQL Configuration
# SECURITY: Use non-standard usernames and strong passwords
# Password must be set via: $env:TF_VAR_sql_admin_password = "YourSecurePassword123!"
sql_admin_username = "cvadmin_test" # Avoid common names like 'admin', 'sa', 'sqladmin'
