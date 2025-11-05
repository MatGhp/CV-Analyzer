# Development Environment Configuration

# Resource Location
environment = "dev"
location    = "swedencentral"

# SQL Configuration
sql_admin_username = "cvadmin_dev"  # Non-standard username for security
# Note: SQL admin password must be set via environment variable:
# $env:TF_VAR_sql_admin_password = "YourSecurePassword123!"

# Container Registry Configuration
acr_sku = "Basic"  # Basic SKU for dev environment

# Container Apps Configuration
min_replicas = 0 # Enable scale-to-zero for cost savings in dev
max_replicas = 3 # Lower max for dev environment

# AI Configuration
model_deployment_name = "gpt-4o"
model_capacity        = 10 # 10k tokens/min for dev

# Docker Image Configuration
image_tag = "latest"

