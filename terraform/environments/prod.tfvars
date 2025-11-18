# Production Environment Configuration

environment = "prod"
location    = "germanywestcentral"

# SQL Configuration
# SECURITY: Use non-standard usernames and strong passwords.
# Set the SQL admin password locally as an environment variable; do not include commands or names in commits.
sql_admin_username = "cvadmin_prod" # Avoid common names like 'admin', 'sa', 'sqladmin'
sql_database_sku   = "S1"           # S1 SKU for production (better performance)

# Container Registry Configuration
acr_sku = "Standard" # Standard SKU for better performance and geo-replication in prod

# Container Apps Configuration
min_replicas = 1  # Always-on for production
max_replicas = 10 # Higher capacity for production load

# AI Configuration
model_deployment_name = "gpt-4o"
model_capacity        = 30 # 30k tokens/min for production

# Docker Image Configuration
image_tag = "latest" # Use specific version tags in production (e.g., "v1.0.0")

