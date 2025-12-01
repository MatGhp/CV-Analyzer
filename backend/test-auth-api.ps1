# Test Authentication API Endpoints
# Run this after starting the API with 'dotnet run'

$apiUrl = "https://localhost:7080"  # Update port if different

Write-Host "`n=== Testing CV Analyzer Authentication API ===" -ForegroundColor Cyan

# Test 1: Register a new user
Write-Host "`n1. Registering new user..." -ForegroundColor Yellow
$registerBody = @{
    email = "test@example.com"
    password = "Test@123456"
    fullName = "Test User"
    phone = "+1234567890"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$apiUrl/api/auth/register" -Method Post -Body $registerBody -ContentType "application/json"
    Write-Host "✅ Registration successful!" -ForegroundColor Green
    Write-Host "UserId: $($registerResponse.userId)" -ForegroundColor Gray
    Write-Host "Token: $($registerResponse.token.Substring(0, 20))..." -ForegroundColor Gray
    
    $token = $registerResponse.token
    $userId = $registerResponse.userId
} catch {
    Write-Host "❌ Registration failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 2: Get user profile
Write-Host "`n2. Getting user profile..." -ForegroundColor Yellow
$headers = @{
    "Authorization" = "Bearer $token"
}

try {
    $profileResponse = Invoke-RestMethod -Uri "$apiUrl/api/auth/me" -Method Get -Headers $headers
    Write-Host "✅ Profile retrieved!" -ForegroundColor Green
    Write-Host "Email: $($profileResponse.email)" -ForegroundColor Gray
    Write-Host "Full Name: $($profileResponse.fullName)" -ForegroundColor Gray
    Write-Host "Total Resumes: $($profileResponse.totalResumes)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Profile retrieval failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Get user resumes
Write-Host "`n3. Getting user resumes..." -ForegroundColor Yellow
try {
    $resumesResponse = Invoke-RestMethod -Uri "$apiUrl/api/auth/me/resumes" -Method Get -Headers $headers
    Write-Host "✅ Resumes retrieved!" -ForegroundColor Green
    Write-Host "Resume count: $($resumesResponse.Count)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Resume retrieval failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Login with registered user
Write-Host "`n4. Testing login..." -ForegroundColor Yellow
$loginBody = @{
    email = "test@example.com"
    password = "Test@123456"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$apiUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    Write-Host "✅ Login successful!" -ForegroundColor Green
    Write-Host "UserId: $($loginResponse.userId)" -ForegroundColor Gray
    Write-Host "Token: $($loginResponse.token.Substring(0, 20))..." -ForegroundColor Gray
    Write-Host "Last Login: $($loginResponse.lastLoginAt)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: Test invalid credentials
Write-Host "`n5. Testing invalid credentials..." -ForegroundColor Yellow
$invalidLoginBody = @{
    email = "test@example.com"
    password = "WrongPassword"
} | ConvertTo-Json

try {
    $invalidLoginResponse = Invoke-RestMethod -Uri "$apiUrl/api/auth/login" -Method Post -Body $invalidLoginBody -ContentType "application/json"
    Write-Host "❌ Should have failed with invalid credentials!" -ForegroundColor Red
} catch {
    Write-Host "✅ Correctly rejected invalid credentials" -ForegroundColor Green
}

# Test 6: Test unauthorized access
Write-Host "`n6. Testing unauthorized access (no token)..." -ForegroundColor Yellow
try {
    $unauthorizedResponse = Invoke-RestMethod -Uri "$apiUrl/api/auth/me" -Method Get
    Write-Host "❌ Should have returned 401 Unauthorized!" -ForegroundColor Red
} catch {
    Write-Host "✅ Correctly blocked unauthorized access" -ForegroundColor Green
}

Write-Host "`n=== All tests completed ===" -ForegroundColor Cyan
