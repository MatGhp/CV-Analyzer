# Stage 1: Build the Angular application
FROM node:20-alpine AS build

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Build the application for production
RUN npm run build -- --configuration=production

# Stage 2: Serve with nginx
FROM nginx:1.25-alpine
RUN apk add --no-cache gettext

# Copy nginx configuration template (will be processed by entrypoint script)
COPY nginx.conf.template /etc/nginx/nginx.conf.template

# Copy startup script with execute permissions
COPY --chmod=755 docker-entrypoint.sh /docker-entrypoint.sh

# Copy built application from build stage
COPY --from=build /app/dist/cv-analyzer-frontend/browser /usr/share/nginx/html

# Expose port 80
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost/health || exit 1

# Use custom entrypoint that generates nginx.conf from template
ENTRYPOINT ["/docker-entrypoint.sh"]
