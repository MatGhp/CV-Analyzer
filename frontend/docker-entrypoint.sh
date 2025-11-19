#!/bin/sh
set -e

# Verify API_FQDN is set before proceeding
if [ -z "$API_FQDN" ]; then
    echo "ERROR: API_FQDN environment variable is not set."
    echo "This variable should be automatically injected by Azure Container Apps from Terraform configuration."
    echo "Check: terraform/modules/container-apps/main.tf (env block) for correct definition."
    echo "If missing, verify your Terraform deployment and CI/CD pipeline are passing the correct value."
    exit 1
fi

# Validate FQDN format (DNS-compliant: labels 1-63 chars, alphanumeric, hyphens only in middle, no consecutive dots)
if ! echo "$API_FQDN" | grep -qE '^([a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$'; then
    echo "ERROR: Invalid API_FQDN format: $API_FQDN"
    exit 1
fi

# Substitute environment variables in nginx.conf.template
# API_FQDN is passed from Container Apps environment variable
envsubst '${API_FQDN}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

# Validate nginx syntax before starting
nginx -t || {
    echo "ERROR: Generated nginx.conf is invalid"
    echo "=== Generated Configuration ==="
    cat /etc/nginx/nginx.conf
    echo "=============================="
    exit 1
}

echo "=== Frontend Container Startup ==="
echo "API_FQDN: $API_FQDN"
echo "Testing API connectivity..."
if ! wget --spider --timeout=5 "https://$API_FQDN/api/health" 2>&1 | tee /tmp/health-check.log; then
    echo "WARNING: API health check failed at startup (may not be ready yet)"
    echo "Error details:"
    cat /tmp/health-check.log
fi
echo "nginx configuration validated successfully"
echo "=================================="

# Start nginx in the foreground
exec nginx -g 'daemon off;'
