# Task 2: Backend Core Implementation

**Estimated Time**: 1 day  
**Priority**: P0 (Blocking for all other tasks)  
**Dependencies**: Task 1 (Infrastructure Setup) ✅ Complete  
**Last Reviewed**: November 13, 2025

---

## ⚠️ Implementation Status

This task document has been **reviewed against actual deployed infrastructure** and existing codebase:

**Infrastructure (Task 1) - ✅ DEPLOYED:**
- Storage account `cvanalyzerdevs4b3` (Standard_LRS, swedencentral)
- Blob container `resumes` exists
- Document Intelligence `cvanalyzer-dev-docintel` (FormRecognizer F0, swedencentral)
- Queues `resume-analysis` and `resume-analysis-poison` exist

**Backend Code - ⚠️ PARTIAL:**
- ✅ BlobStorageService skeleton exists but needs Azure SDK implementation
- ✅ Resume entity exists but needs schema updates (enum, BlobUrlWithSas, CandidateInfo)
- ❌ DocumentIntelligenceService does NOT exist yet (must create)
- ❌ CandidateInfo entity does NOT exist yet (must create)
- ❌ NuGet packages not installed (Azure.Storage.Blobs, Azure.AI.FormRecognizer)

**Action Required**: Follow deliverables below to complete implementation against deployed infrastructure.

---

## Overview

Implement core backend services for blob storage, document text extraction, and database updates. This task establishes the foundation for async CV processing.

---

## Prerequisites

✅ Task 1 completed - Verified resources:
- ✅ Storage account `cvanalyzerdevs4b3` deployed (Standard_LRS, swedencentral)
- ✅ Blob container `resumes` created (private access)
- ✅ Document Intelligence `cvanalyzer-dev-docintel` deployed (FormRecognizer F0, swedencentral)
  - Endpoint: `https://swedencentral.api.cognitive.microsoft.com/`
- ✅ Queues `resume-analysis` and `resume-analysis-poison` created
- ✅ Storage endpoints available:
  - Blob: `https://cvanalyzerdevs4b3.blob.core.windows.net/`
  - Queue: `https://cvanalyzerdevs4b3.queue.core.windows.net/`

---

## Deliverables

### 1. Secure Blob Storage Service with SAS Tokens

**File**: `backend/src/CVAnalyzer.Infrastructure/Services/BlobStorageService.cs` ⚠️ **EXISTS - NEEDS REPLACEMENT**

**Current Implementation**: Stub methods returning fake URLs  
**Action**: Replace entire implementation with real Azure SDK code

**Requirements**:
- ✅ Upload files to `resumes` container
- ✅ Generate SAS tokens using **user delegation key** (managed identity support)
- ✅ 24-hour expiration with 5-minute clock skew tolerance
- ✅ Read-only permissions on SAS tokens
- ✅ Return both permanent URL and URL with SAS token
- ✅ Content-Type detection (.pdf, .docx)
- ✅ Delete file support

**⚠️ Interface Changes Required**:

Current interface:
```csharp
public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string blobUrl, CancellationToken cancellationToken = default);
    Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default);
}
```

**Update to**:
```csharp
public interface IBlobStorageService
{
    Task<BlobUploadResult> UploadFileAsync(Stream fileStream, string fileName);
    Task<bool> DeleteFileAsync(string blobUrl);
}

public record BlobUploadResult(string BlobUrl, string BlobUrlWithSas);
```

**Key Implementation Details**:
```csharp
public interface IBlobStorageService
{
    Task<BlobUploadResult> UploadFileAsync(Stream fileStream, string fileName);
    Task<bool> DeleteFileAsync(string blobUrl);
}

public record BlobUploadResult(string BlobUrl, string BlobUrlWithSas);
```

**Why User Delegation Key?**
- Works with managed identity (no storage account keys needed)
- More secure than account key SAS tokens
- Required pattern for Azure-recommended passwordless authentication

**Testing**:

```bash
# Manual test (update with actual Container App URL once deployed)
curl -X POST https://ca-cvanalyzer-api.swedencentral.azurecontainerapps.io/api/resumes/upload \
  -F "file=@sample-resume.pdf" \
  -F "userId=test-user-123"

# Verify blob created and SAS token works
curl "<blob-url-with-sas>" --output downloaded.pdf

# Verify blob container exists
az storage container show --name resumes --account-name cvanalyzerdevs4b3 --auth-mode login
```

---

### 2. Document Intelligence Service

**File**: `backend/src/CVAnalyzer.Infrastructure/Services/DocumentIntelligenceService.cs` ❌ **DOES NOT EXIST - MUST CREATE**

**Action**: Create new file and interface from scratch

**Requirements**:
- ✅ Extract text from PDF/DOCX using Document Intelligence (FormRecognizer)
- ✅ Use `prebuilt-read` model (optimized for text extraction)
- ✅ Accept blob URL with SAS token
- ✅ Return extracted text preserving layout (pages → lines → content)
- ✅ Handle API failures gracefully with clear error messages
- ✅ Log character count for monitoring

**Key Implementation Details**:
```csharp
public interface IDocumentIntelligenceService
{
    Task<string> ExtractTextFromDocumentAsync(string blobUrlWithSas);
}
```

**Why Document Intelligence?**
- GPT-4o vision **cannot** read PDF/DOCX binary formats
- Handles complex layouts (multi-column, tables, headers/footers)
- OCR capability for scanned documents
- Industry-standard solution ($15/month for 1000 docs)

**Testing**:
```csharp
// Integration test
var samplePdfUrl = "https://cvanalyzerdevs4b3.blob.core.windows.net/resumes/sample.pdf?<sas>";
var extractedText = await _documentIntelligenceService.ExtractTextFromDocumentAsync(samplePdfUrl);

Assert.True(extractedText.Length > 100);
Assert.Contains("Experience", extractedText, StringComparison.OrdinalIgnoreCase);
```

---

### 3. Database Updates

#### 3.1 New Entity: CandidateInfo

**File**: `backend/src/CVAnalyzer.Domain/Entities/CandidateInfo.cs` ❌ **DOES NOT EXIST - MUST CREATE**

**Action**: Create new entity file in Domain/Entities folder

**Requirements**:
- ✅ Store extracted candidate information
- ✅ One-to-one relationship with Resume
- ✅ Skills stored as JSON array (use EF Core value converter)
- ✅ Include CreatedAt and UpdatedAt timestamps

**Schema**:
```csharp
public class CandidateInfo
{
    public Guid Id { get; set; }
    public Guid ResumeId { get; set; }
    
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public List<string> Skills { get; set; } = new();
    public int? YearsOfExperience { get; set; }
    public string? CurrentJobTitle { get; set; }
    public string? Education { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public Resume Resume { get; set; } = null!;
}
```

#### 3.2 Update Resume Entity

**File**: `backend/src/CVAnalyzer.Domain/Entities/Resume.cs` ⚠️ **EXISTS - NEEDS UPDATES**

**Current Schema**:
```csharp
public class Resume : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string BlobStorageUrl { get; set; } = string.Empty;  // ⚠️ RENAME to BlobUrl
    public string OriginalContent { get; set; } = string.Empty;
    public string? OptimizedContent { get; set; }
    public string Status { get; set; } = "Pending";  // ⚠️ CHANGE to enum
    public double? Score { get; set; }
    public ICollection<Suggestion> Suggestions { get; set; } = new List<Suggestion>();
}
```

**Changes Required**:
- ✅ Rename `BlobStorageUrl` → `BlobUrl` (consistency)
- ✅ Add `BlobUrlWithSas` property (stores URL with SAS token for 24h)
- ✅ Change `Status` from `string` to `ResumeStatus` enum
- ✅ Add navigation property to `CandidateInfo`

**Updated Schema**:
```csharp
public class Resume
{
    // Existing properties...
    public string BlobUrl { get; set; } = string.Empty;
    public string BlobUrlWithSas { get; set; } = string.Empty; // NEW
    public ResumeStatus Status { get; set; } = ResumeStatus.Pending; // NEW
    
    // Navigation
    public CandidateInfo? CandidateInfo { get; set; } // NEW
    public ICollection<Suggestion> Suggestions { get; set; } = new List<Suggestion>();
}

public enum ResumeStatus
{
    Pending = 0,
    Processing = 1,
    Analyzed = 2,
    Failed = 3
}
```

#### 3.3 EF Core Configuration

**File**: `backend/src/CVAnalyzer.Infrastructure/Persistence/ApplicationDbContext.cs`

**Updates**:
```csharp
public DbSet<CandidateInfo> CandidateInfo => Set<CandidateInfo>();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // CandidateInfo configuration
    modelBuilder.Entity<CandidateInfo>(entity =>
    {
        entity.HasKey(c => c.Id);
        entity.HasIndex(c => c.ResumeId).IsUnique();
        
        // Skills stored as JSON
        entity.Property(c => c.Skills)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());
        
        entity.HasOne(c => c.Resume)
            .WithOne(r => r.CandidateInfo)
            .HasForeignKey<CandidateInfo>(c => c.ResumeId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}
```

#### 3.4 Database Migration

**Commands**:
```bash
cd backend/src/CVAnalyzer.Infrastructure
dotnet ef migrations add AddCandidateInfoAndResumeStatus --startup-project ../CVAnalyzer.API
dotnet ef database update --startup-project ../CVAnalyzer.API
```

**Verify Migration**:
```sql
-- Check new table
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CandidateInfo';

-- Check Resume columns
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Resumes' 
AND COLUMN_NAME IN ('BlobUrlWithSas', 'Status');
```

---

### 4. NuGet Package Installation

**⚠️ REQUIRED FIRST**: Install Azure SDKs before implementing services

```bash
cd backend/src/CVAnalyzer.Infrastructure

# Azure Storage SDK (Blobs + SAS token generation)
dotnet add package Azure.Storage.Blobs --version 12.19.1

# Document Intelligence SDK (text extraction)
dotnet add package Azure.AI.FormRecognizer --version 4.1.0

# Azure Identity (for managed identity support)
dotnet add package Azure.Identity --version 1.11.0
```

---

### 5. Dependency Injection Updates

**File**: `backend/src/CVAnalyzer.Infrastructure/DependencyInjection.cs` ⚠️ **EXISTS - NEEDS UPDATES**

**Current State**: Only registers stub BlobStorageService and AIResumeAnalyzerService

**Updates Required**:
```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    // Existing SQL Server setup...
    
    // Azure Storage (Blob + Queue) - Managed Identity Support
    var storageAccountName = configuration["AzureStorage:AccountName"]!;
    var useManagedIdentity = configuration.GetValue<bool>("AzureStorage:UseManagedIdentity");
    
    if (useManagedIdentity)
    {
        var blobEndpoint = configuration["AzureStorage:BlobEndpoint"]!;
        var queueEndpoint = configuration["AzureStorage:QueueEndpoint"]!;
        
        services.AddSingleton(sp => new BlobServiceClient(
            new Uri(blobEndpoint),
            new DefaultAzureCredential()));
        
        services.AddSingleton(sp => new QueueServiceClient(
            new Uri(queueEndpoint),
            new DefaultAzureCredential()));
    }
    else
    {
        var connectionString = configuration["AzureStorage:ConnectionString"]!;
        services.AddSingleton(new BlobServiceClient(connectionString));
        services.AddSingleton(new QueueServiceClient(connectionString));
    }
    
    services.AddScoped<IBlobStorageService, BlobStorageService>();
    
    // Document Intelligence
    var docIntelEndpoint = configuration["DocumentIntelligence:Endpoint"]!;
    var docIntelKey = configuration["DocumentIntelligence:ApiKey"]!;
    
    services.AddSingleton(sp => new DocumentAnalysisClient(
        new Uri(docIntelEndpoint),
        new AzureKeyCredential(docIntelKey)));
    
    services.AddScoped<IDocumentIntelligenceService, DocumentIntelligenceService>();
    
    return services;
}
```

**Configuration (appsettings.json)** - Update with actual deployed resources:

```json
{
  "AzureStorage": {
    "UseManagedIdentity": false,
    "ConnectionString": "<from-azure-cli-or-key-vault>",
    "AccountName": "cvanalyzerdevs4b3",
    "BlobEndpoint": "https://cvanalyzerdevs4b3.blob.core.windows.net/",
    "QueueEndpoint": "https://cvanalyzerdevs4b3.queue.core.windows.net/",
    "QueueName": "resume-analysis",
    "PoisonQueueName": "resume-analysis-poison",
    "ContainerName": "resumes"
  },
  "DocumentIntelligence": {
    "Endpoint": "https://swedencentral.api.cognitive.microsoft.com/",
    "ApiKey": "<from-azure-cli-or-key-vault>"
  }
}
```

**Get connection string (for local development)**:

```bash
# Storage connection string
az storage account show-connection-string --name cvanalyzerdevs4b3 --resource-group rg-cvanalyzer-dev --query connectionString --output tsv

# Document Intelligence key
az cognitiveservices account keys list --name cvanalyzer-dev-docintel --resource-group rg-cvanalyzer-dev --query key1 --output tsv
```

---

## Unit Tests

### BlobStorageServiceTests

**File**: `backend/tests/CVAnalyzer.UnitTests/Services/BlobStorageServiceTests.cs`

**Test Cases**:
- ✅ `UploadFileAsync_ValidPdf_ReturnsBlobUrls`
- ✅ `UploadFileAsync_GeneratesSasToken_WithCorrectPermissions`
- ✅ `DeleteFileAsync_ExistingBlob_ReturnsTrue`
- ✅ `GetContentType_PdfExtension_ReturnsCorrectMimeType`

### DocumentIntelligenceServiceTests

**File**: `backend/tests/CVAnalyzer.UnitTests/Services/DocumentIntelligenceServiceTests.cs`

**Test Cases**:
- ✅ `ExtractTextFromDocumentAsync_ValidPdf_ReturnsText`
- ✅ `ExtractTextFromDocumentAsync_ApiFailure_ThrowsInvalidOperationException`
- ✅ `ExtractTextFromDocumentAsync_PreservesLayout`

---

## Integration Tests

**File**: `backend/tests/CVAnalyzer.IntegrationTests/Services/BlobAndDocumentIntelligenceIntegrationTests.cs`

**Test Scenario**: Full upload + extraction flow
```csharp
[Fact]
public async Task FullFlow_UploadAndExtract_Success()
{
    // Arrange
    var samplePdfPath = "TestData/sample-resume.pdf";
    using var fileStream = File.OpenRead(samplePdfPath);
    
    // Act - Upload
    var uploadResult = await _blobStorageService.UploadFileAsync(
        fileStream, "sample-resume.pdf");
    
    Assert.NotEmpty(uploadResult.BlobUrl);
    Assert.Contains("?sv=", uploadResult.BlobUrlWithSas); // Has SAS
    
    // Act - Extract
    var extractedText = await _documentIntelligenceService
        .ExtractTextFromDocumentAsync(uploadResult.BlobUrlWithSas);
    
    // Assert
    Assert.True(extractedText.Length > 100);
    Assert.Contains("Experience", extractedText, StringComparison.OrdinalIgnoreCase);
    
    // Cleanup
    await _blobStorageService.DeleteFileAsync(uploadResult.BlobUrl);
}
```

---

## Acceptance Criteria

### Current Implementation Status

**✅ Already Implemented:**
- [x] BlobStorageService skeleton exists (`Infrastructure/Services/BlobStorageService.cs`)
- [x] IBlobStorageService interface exists (with `UploadAsync`, `DownloadAsync`, `DeleteAsync`)
- [x] Resume entity exists with `Status` field (currently string type)
- [x] DependencyInjection configured with Key Vault support
- [x] Infrastructure resources deployed (storage account, Document Intelligence, queues)

**❌ Needs Implementation:**
- [ ] **BlobStorageService**: Replace stub with real Azure SDK implementation
  - [ ] Add Azure.Storage.Blobs NuGet package
  - [ ] Implement actual blob upload to `cvanalyzerdevs4b3/resumes` container
  - [ ] Implement SAS token generation with user delegation key
  - [ ] Update interface to return `BlobUploadResult` record with both URLs
- [ ] **DocumentIntelligenceService**: Create new service (doesn't exist yet)
  - [ ] Add Azure.AI.FormRecognizer NuGet package
  - [ ] Create `IDocumentIntelligenceService` interface
  - [ ] Implement text extraction using `prebuilt-read` model
  - [ ] Register in DependencyInjection.cs
- [ ] **CandidateInfo Entity**: Create new entity
  - [ ] Add `CandidateInfo.cs` to Domain/Entities
  - [ ] Configure EF Core relationship with Resume (1:1)
  - [ ] Add JSON value converter for Skills list
- [ ] **Resume Entity Updates**:
  - [ ] Rename `BlobStorageUrl` → `BlobUrl`
  - [ ] Add `BlobUrlWithSas` property
  - [ ] Change `Status` from `string` to `ResumeStatus` enum
  - [ ] Add `CandidateInfo` navigation property
- [ ] **Database Migration**: Create and apply migration
- [ ] **Unit Tests**: Create test projects (20+ tests)
- [ ] **Integration Tests**: Full upload + extraction flow

---

## Rollback Plan

1. Revert database migration: `dotnet ef migrations remove`
2. Remove new services from DI registration
3. Revert Resume entity changes (keep backward compatibility)
4. No data loss risk (blob storage independent)

---

## Quick Reference: Files to Create/Update

### Create New Files (❌):
1. `backend/src/CVAnalyzer.Infrastructure/Services/DocumentIntelligenceService.cs`
2. `backend/src/CVAnalyzer.Application/Common/Interfaces/IDocumentIntelligenceService.cs`
3. `backend/src/CVAnalyzer.Domain/Entities/CandidateInfo.cs`
4. Database migration file (via `dotnet ef migrations add`)

### Update Existing Files (⚠️):
1. `backend/src/CVAnalyzer.Infrastructure/Services/BlobStorageService.cs` - Replace stub with Azure SDK
2. `backend/src/CVAnalyzer.Application/Common/Interfaces/IBlobStorageService.cs` - Update interface signature
3. `backend/src/CVAnalyzer.Domain/Entities/Resume.cs` - Add fields, change Status to enum
4. `backend/src/CVAnalyzer.Infrastructure/Persistence/ApplicationDbContext.cs` - Add DbSet and configuration
5. `backend/src/CVAnalyzer.Infrastructure/DependencyInjection.cs` - Register BlobServiceClient, QueueServiceClient, DocumentAnalysisClient
6. `backend/src/CVAnalyzer.API/appsettings.json` - Update with actual endpoint URLs
7. `backend/src/CVAnalyzer.API/appsettings.Development.json` - Add connection strings for local dev

### Install NuGet Packages:
```bash
dotnet add package Azure.Storage.Blobs --version 12.19.1
dotnet add package Azure.AI.FormRecognizer --version 4.1.0
dotnet add package Azure.Identity --version 1.11.0
```

---

## Next Steps

After Task 2 completion:
- **Task 3**: Queue + Background Worker (async processing)
- **Task 4**: API Updates (upload handler, status endpoint)
- **Task 5**: Frontend (status polling, candidate info display)
