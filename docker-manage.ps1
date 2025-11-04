# CV Analyzer - Docker Build & Deploy Script
# PowerShell script for building and deploying the CV Analyzer platform

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('dev', 'prod')]
    [string]$Environment = 'dev',
    
    [Parameter(Mandatory=$false)]
    [switch]$Build,
    
    [Parameter(Mandatory=$false)]
    [switch]$Up,
    
    [Parameter(Mandatory=$false)]
    [switch]$Down,
    
    [Parameter(Mandatory=$false)]
    [switch]$Logs
)

$ErrorActionPreference = "Stop"

Write-Host "CV Analyzer - Docker Management" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host ""

# Check if .env file exists
if (-not (Test-Path ".env")) {
    Write-Host "Warning: .env file not found. Creating from .env.example..." -ForegroundColor Yellow
    if (Test-Path ".env.example") {
        Copy-Item ".env.example" ".env"
        Write-Host "Please update .env with your actual values before running services." -ForegroundColor Red
        exit 1
    }
}

# Select compose file
$composeFile = if ($Environment -eq 'prod') { 'docker-compose.prod.yml' } else { 'docker-compose.yml' }

if ($Build) {
    Write-Host "Building all services..." -ForegroundColor Green
    docker-compose -f $composeFile build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "Build completed successfully!" -ForegroundColor Green
}

if ($Up) {
    Write-Host "Starting services..." -ForegroundColor Green
    docker-compose -f $composeFile up -d
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to start services!" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host ""
    Write-Host "Services started successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Access the application:" -ForegroundColor Cyan
    Write-Host "  Frontend:        http://localhost:4200" -ForegroundColor White
    Write-Host "  API Swagger:     http://localhost:5000/swagger" -ForegroundColor White
    Write-Host "  AI Service Docs: http://localhost:8000/docs" -ForegroundColor White
    Write-Host ""
    Write-Host "View logs with: docker-compose logs -f" -ForegroundColor Gray
}

if ($Down) {
    Write-Host "Stopping services..." -ForegroundColor Yellow
    docker-compose -f $composeFile down
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to stop services!" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "Services stopped!" -ForegroundColor Green
}

if ($Logs) {
    Write-Host "Showing logs (Ctrl+C to exit)..." -ForegroundColor Cyan
    docker-compose -f $composeFile logs -f
}

if (-not ($Build -or $Up -or $Down -or $Logs)) {
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\docker-manage.ps1 -Build           # Build all containers" -ForegroundColor White
    Write-Host "  .\docker-manage.ps1 -Up              # Start all services" -ForegroundColor White
    Write-Host "  .\docker-manage.ps1 -Down            # Stop all services" -ForegroundColor White
    Write-Host "  .\docker-manage.ps1 -Logs            # View logs" -ForegroundColor White
    Write-Host "  .\docker-manage.ps1 -Build -Up       # Build and start" -ForegroundColor White
    Write-Host "  .\docker-manage.ps1 -Environment prod -Up  # Use production config" -ForegroundColor White
}
