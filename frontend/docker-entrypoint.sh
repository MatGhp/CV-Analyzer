#!/bin/sh
set -e

# Verify API_FQDN is set before proceeding
if [ -z "$API_FQDN" ]; then
    echo "ERROR: API_FQDN environment variable is not set"
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
wget --spider --timeout=5 "https://$API_FQDN/health" 2>&1 || echo "WARNING: API health check failed at startup (may not be ready yet)"
echo "nginx configuration validated successfully"
echo "=================================="

# Start nginx in the foreground
exec nginx -g 'daemon off;'
