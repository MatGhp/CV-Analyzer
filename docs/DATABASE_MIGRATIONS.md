# Database Migrations Guide

## Overview

This guide explains how to manage EF Core database migrations for the CV Analyzer application.

## Current Configuration (v1.1 - Automatic Migrations)

**As of November 2025**, the application automatically applies pending database migrations on startup. This ensures production databases stay synchronized with the codebase.

### Startup Behavior

When the API starts, it:
1. Checks for pending migrations
2. Applies them automatically if found
3. Logs the migration activity
4. **Fails startup** if migrations fail (safe-fail pattern)

**Logs example:**
```
[INF] Checking for pending database migrations...
[INF] Applying 1 pending migration(s): 20251113173403_RefactoredTask2Implementation
[INF] Database migrations applied successfully
```

### Why Automatic Migrations?

**Pros:**
- ✅ Zero-downtime deployments (migrations run before serving traffic)
- ✅ No manual intervention needed
- ✅ Prevents "Invalid object name" errors in production
- ✅ Works seamlessly with Container Apps revisions

**Cons:**
- ⚠️ Can delay startup time (typically 5-15 seconds)
- ⚠️ Requires careful migration testing in lower environments
- ⚠️ Concurrent container startups may cause race conditions (EF Core handles this with retry logic)

### When to Disable Automatic Migrations

For **large-scale production systems** with:
- High-traffic APIs that need instant startup
- Complex migrations requiring downtime windows
- Strict change control processes

In these cases, use **Manual Migration Deployment** (see Option 2 below).

---

## Creating New Migrations

### Prerequisites
- .NET 10 SDK installed
- EF Core tools: `dotnet tool install --global dotnet-ef`

### Steps

1. **Make changes to domain entities** in `backend/src/CVAnalyzer.Domain/Entities/`

2. **Add migration** (from `backend/` directory):
   ```powershell
   dotnet ef migrations add <MigrationName> `
     --project src/CVAnalyzer.Infrastructure `
     --startup-project src/CVAnalyzer.API
   ```

3. **Review generated migration** in `src/CVAnalyzer.Infrastructure/Migrations/`

4. **Test migration locally**:
   ```powershell
   dotnet ef database update `
     --project src/CVAnalyzer.Infrastructure `
     --startup-project src/CVAnalyzer.API
   ```

5. **Commit migration files**:
   ```bash
   git add backend/src/CVAnalyzer.Infrastructure/Migrations/
   git commit -m "feat(backend): add <feature> migration"
   ```

### Migration Naming Convention

Use descriptive names that explain the change:
- ✅ `AddCandidateInfoTable`
- ✅ `AddResumeAnalysisStatus`
- ✅ `RefactorSuggestionsRelationship`
- ❌ `UpdateDatabase`
- ❌ `Changes`

---

## Deployment Options

### Option 1: Automatic Migrations (Current - Recommended for MVP)

**Configuration:** Already enabled in `Program.cs` (as of v1.1)

**How it works:**
1. CI/CD deploys new container image to Container Apps
2. Container starts → runs `MigrateAsync()` before serving traffic
3. Health probes wait until startup completes
4. Traffic routes to new revision only after migrations succeed

**No additional steps needed** - migrations apply automatically on deployment.

**Rollback:** If migration fails, container crashes → Azure keeps previous revision active.

---

### Option 2: Manual Migration Deployment (Enterprise Pattern)

For production systems requiring explicit migration control:

#### Step 1: Disable Automatic Migrations

Comment out the migration block in `backend/src/CVAnalyzer.API/Program.cs`:

```csharp
// Apply database migrations automatically
// using (var scope = app.Services.CreateScope())
// {
//     // ... migration code ...
// }
```

#### Step 2: Run Migrations via Azure CLI

**From local machine (requires Azure CLI + DB access):**

```powershell
# Set connection string from Key Vault
$connString = az keyvault secret show `
  --vault-name kv-cvanalyzer-prod `
  --name DatabaseConnectionString `
  --query value -o tsv

# Run migrations
$env:ConnectionStrings__DefaultConnection = $connString
cd backend
dotnet ef database update `
  --project src/CVAnalyzer.Infrastructure `
  --startup-project src/CVAnalyzer.API
```

**From CI/CD pipeline (GitHub Actions):**

Add a manual workflow `.github/workflows/db-migrate.yml`:

```yaml
name: Database Migration

on:
  workflow_dispatch:
    inputs:
      environment:
        description: 'Environment (dev/test/prod)'
        required: true
        type: choice
        options:
          - dev
          - test
          - prod

jobs:
  migrate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Get DB Connection String
        id: db-conn
        run: |
          CONN_STRING=$(az keyvault secret show \
            --vault-name kv-cvanalyzer-${{ inputs.environment }} \
            --name DatabaseConnectionString \
            --query value -o tsv)
          echo "::add-mask::$CONN_STRING"
          echo "CONNECTION_STRING=$CONN_STRING" >> $GITHUB_OUTPUT
      
      - name: Run Migrations
        working-directory: backend
        env:
          ConnectionStrings__DefaultConnection: ${{ steps.db-conn.outputs.CONNECTION_STRING }}
        run: |
          dotnet ef database update \
            --project src/CVAnalyzer.Infrastructure \
            --startup-project src/CVAnalyzer.API
```

**Usage:**
1. Go to GitHub Actions → "Database Migration" workflow
2. Click "Run workflow"
3. Select environment (dev/test/prod)
4. Wait for migration to complete
5. Deploy application code

---

## Troubleshooting

### Error: "Invalid object name 'Resumes'"

**Cause:** Database schema not created (migrations not applied)

**Solution:**
- With automatic migrations: Redeploy application (migration runs on startup)
- With manual migrations: Run `dotnet ef database update` manually

### Error: "Migration already applied"

**Cause:** Attempting to apply migration that's already in database

**Solution:**
```powershell
# Check migration status
dotnet ef migrations list `
  --project src/CVAnalyzer.Infrastructure `
  --startup-project src/CVAnalyzer.API

# If needed, rollback
dotnet ef database update <PreviousMigrationName> `
  --project src/CVAnalyzer.Infrastructure `
  --startup-project src/CVAnalyzer.API
```

### Error: "Timeout connecting to database"

**Cause:** SQL Server firewall blocking IP address

**Solution:**
```powershell
# Add your IP to SQL Server firewall
az sql server firewall-rule create \
  --resource-group rg-cvanalyzer-prod \
  --server sql-cvanalyzer-prod \
  --name AllowMyIP \
  --start-ip-address <YOUR_IP> \
  --end-ip-address <YOUR_IP>
```

### Error: "Concurrent migration execution detected"

**Cause:** Multiple container instances starting simultaneously

**Solution:**
- **Automatic migrations:** EF Core retries automatically (no action needed)
- **Manual migrations:** Use Container Apps' "Single Revision Mode" during deployment
  ```bash
  az containerapp revision set-mode \
    --name ca-cvanalyzer-api \
    --resource-group rg-cvanalyzer-prod \
    --mode Single
  ```

---

## Best Practices

### Development
- ✅ Always create migrations locally before committing
- ✅ Test migrations with real data in dev environment
- ✅ Review generated SQL (`dotnet ef migrations script`) before production
- ✅ Name migrations descriptively
- ❌ Don't edit old migrations (create new ones instead)

### Production
- ✅ Test migrations in dev → test → prod sequence
- ✅ Monitor migration duration (add logging)
- ✅ Have rollback plan (previous migration name ready)
- ✅ Apply migrations during low-traffic windows (if manual)
- ❌ Don't skip environments (always test first)

### CI/CD
- ✅ Automatic migrations work for most scenarios
- ✅ Switch to manual migrations only when needed (scale/complexity)
- ✅ Add migration validation step in CI pipeline
- ❌ Don't allow direct database access from developer machines in production

---

## Migration History

| Date | Migration | Description |
|------|-----------|-------------|
| 2024-11-13 | `RefactoredTask2Implementation` | Initial schema: Resumes, Suggestions, CandidateInfos |

---

## References

- [EF Core Migrations Overview](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
- [Azure SQL Database Deployment](https://learn.microsoft.com/azure/sql-database/sql-database-manage-automation)
- [Container Apps Health Probes](https://learn.microsoft.com/azure/container-apps/health-probes)
- [Clean Architecture Database Patterns](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/architectural-principles#clean-architecture)
