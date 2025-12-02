# Development Environment Configuration

# Azure Subscription
subscription_id = "9bf7d398-40c9-420e-8331-563f3e0dc68f"

# Resource Location
environment = "dev"
location    = "swedencentral"

# SQL Configuration
sql_admin_username = "cvadmin_dev" # Non-standard username for security
sql_database_sku   = "Basic"       # Basic SKU for dev (cost-effective)
# Note: Set the SQL admin password via a local environment variable on your machine (do not commit real secrets or commands).

# Container Registry Configuration
acr_sku = "Basic" # Basic SKU for dev environment

# Container Apps Configuration
min_replicas = 0 # Enable scale-to-zero for cost savings in dev
max_replicas = 3 # Lower max for dev environment
# Health probes: Disabled by default for dev environment during bootstrap (see main.tf logic)
# After first successful deployment with real container images, enable health probes:
# enable_health_probes = true

# AI Configuration
model_deployment_name = "gpt-4o"
model_capacity        = 10 # 10k tokens/min for dev

# Docker Image Configuration
image_tag = "latest"

# SQL Firewall Rules
# Add your IP address for local development access
# Create a dev.local.tfvars file (gitignored) with your actual IP:
# sql_firewall_rules = {
#   "AllowDevMachine" = {
#     start_ip_address = "YOUR_IP_HERE"
#     end_ip_address   = "YOUR_IP_HERE"
#   }
# }
sql_firewall_rules = {}

# GitHub Actions Service Principal Object ID
# Required for CI/CD deployments to access Key Vault secrets
github_actions_sp_object_id = "69080a52-ea0f-4293-84e4-415c1ed697f2"
