# Multi-stage Dockerfile for CV Analyzer AI Service
# Optimized for production with minimal image size

# ============================================================================
# Stage 1: Builder
# ============================================================================
FROM python:3.11-slim as builder

WORKDIR /build

# Install build dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    gcc \
    g++ \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements first for better caching
COPY requirements.txt .

# Install Python dependencies
# IMPORTANT: Use --pre flag for Agent Framework preview package
RUN pip install --no-cache-dir --upgrade pip && \
    pip install --no-cache-dir --pre agent-framework-azure-ai && \
    pip install --no-cache-dir -r requirements.txt

# ============================================================================
# Stage 2: Runtime
# ============================================================================
FROM python:3.11-slim

WORKDIR /app

# Create non-root user for security
RUN useradd -m -u 1000 appuser && \
    chown -R appuser:appuser /app

# Copy Python packages from builder
COPY --from=builder /usr/local/lib/python3.11/site-packages /usr/local/lib/python3.11/site-packages
COPY --from=builder /usr/local/bin /usr/local/bin

# Copy application code
COPY --chown=appuser:appuser app/ /app/app/

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8000/health')" || exit 1

# Environment variables (override these at runtime)
ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    LOG_LEVEL=INFO \
    AI_FOUNDRY_ENDPOINT="" \
    MODEL_DEPLOYMENT_NAME="gpt-4o-deployment"

# Run the application
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000", "--log-level", "info"]
