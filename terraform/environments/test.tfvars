# Test Environment Configuration

environment = "test"
location    = "germanywestcentral"

# SQL Configuration
# SECURITY: Use non-standard usernames and strong passwords.
# Set the SQL admin password locally as an environment variable; do not include commands or names in commits.
sql_admin_username = "cvadmin_test" # Avoid common names like 'admin', 'sa', 'sqladmin'

# Container Registry Configuration
acr_sku = "Basic" # Basic SKU for test environment

# Container Apps Configuration
min_replicas = 0 # Enable scale-to-zero for cost savings in test
max_replicas = 5 # Medium capacity for test environment

# AI Configuration
model_deployment_name = "gpt-4o"
model_capacity        = 15 # 15k tokens/min for testing

# Docker Image Configuration
image_tag = "latest"

