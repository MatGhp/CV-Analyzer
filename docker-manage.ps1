<#
.SYNOPSIS
    CV Analyzer - Docker management script for local development and testing.

.DESCRIPTION
    Manages Docker containers for the CV Analyzer platform (Frontend, Backend, SQL Server).
    Supports building images, starting/stopping services, and viewing logs.

.PARAMETER Environment
    Target environment: 'dev' (default) or 'prod'. Selects the appropriate docker-compose file.

.PARAMETER Build
    Build all Docker images without starting services.

.PARAMETER Up
    Start all services in detached mode (background).

.PARAMETER Down
    Stop and remove all running services.

.PARAMETER Logs
    Display real-time logs from all services (Ctrl+C to exit).

.EXAMPLE
    .\docker-manage.ps1 -Build
    Builds all Docker images.

.EXAMPLE
    .\docker-manage.ps1 -Build -Up
    Builds images and starts all services.

.EXAMPLE
    .\docker-manage.ps1 -Up
    Starts services using existing images.

.EXAMPLE
    .\docker-manage.ps1 -Logs
    Displays real-time logs from all running services.

.EXAMPLE
    .\docker-manage.ps1 -Environment prod -Up
    Starts services using production docker-compose configuration.
#>

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
    Get-Help $MyInvocation.MyCommand.Path -Detailed
}
