# Security Guide - CV Analyzer

**Last Updated:** November 7, 2025  
**Review Schedule:** Quarterly

---

## Table of Contents

- [Critical Security Rules](#critical-security-rules)
- [Quick Reference](#quick-reference)
- [Secrets & Credentials Management](#secrets--credentials-management)
- [Pre-Commit Hooks](#pre-commit-hooks)
- [Application Security (.NET)](#application-security-net)
- [Infrastructure Security (Terraform/Azure)](#infrastructure-security-terraformazure)
- [Security Checklist](#security-checklist)
- [Incident Response](#incident-response)
- [Resources](#resources)

---

## Critical Security Rules

### 🔴 NEVER

- ❌ Commit secrets/passwords/API keys/tokens to Git
- ❌ Hardcode subscription IDs, tenant IDs, or client secrets
- ❌ Concatenate user input into SQL queries
- ❌ Expose stack traces in production
- ❌ Disable HTTPS in production
- ❌ Use weak passwords (min 12 chars required)
- ❌ Log sensitive data (passwords, PII, tokens)
- ❌ Bypass authentication or authorization
- ❌ Commit `.tfvars` files with real values
- ❌ Allow unrestricted file uploads

### ✅ ALWAYS

- ✅ Use parameterized queries (Entity Framework)
- ✅ Validate ALL user input with FluentValidation
- ✅ Store secrets in Azure Key Vault
- ✅ Use managed identity for Azure resources
- ✅ Mark sensitive Terraform variables as `sensitive = true`
- ✅ Enable HTTPS enforcement (`https_only = true`)
- ✅ Implement minimum TLS 1.2
- ✅ Enable threat detection for production SQL
- ✅ Add resource locks to production
- ✅ Validate file uploads (type, size, content)

---

## Quick Reference

### Environment-Specific Security

| Security Control | Dev | Test | Prod |
|-----------------|-----|------|------|
| Key Vault Purge Protection | ❌ | ❌ | ✅ |
| Key Vault Network ACLs | Allow All | Allow All | Deny (default) |
| SQL Public Access | ✅ | ✅ | ❌ |
| SQL Threat Detection | ❌ | ✅ | ✅ |
| Resource Locks | ❌ | ❌ | ✅ |
| Detailed Error Messages | ✅ | ⚠️ | ❌ |
| HTTPS Enforcement | ✅ | ✅ | ✅ |
| Minimum TLS Version | 1.2 | 1.2 | 1.2 |

### Pre-Commit Checklist

Before committing code, verify:

- [ ] No hardcoded secrets or credentials
- [ ] All user input is validated
- [ ] SQL queries use parameterization
- [ ] File uploads are validated (type, size, content)
- [ ] Authentication/authorization properly implemented
- [ ] Sensitive data not logged
- [ ] Error messages don't expose internals in production
- [ ] Terraform: No hardcoded subscription IDs
- [ ] Terraform: Sensitive variables marked `sensitive = true`
- [ ] Terraform: Production resources have security controls
- [ ] Terraform: `.tfvars` files not committed (only examples)

---

## Secrets & Credentials Management

### GitHub Secrets

All sensitive values stored as GitHub Secrets (Repository Settings > Secrets):

| Secret Name | Purpose | Rotation Schedule |
|-------------|---------|-------------------|
| `AZURE_CREDENTIALS` | Service principal JSON | Every 90 days |
| `AZURE_CLIENT_ID` | SP client ID | With credentials |
| `AZURE_CLIENT_SECRET` | SP secret | Every 90 days |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription | Rarely changes |
| `AZURE_TENANT_ID` | Azure tenant | Rarely changes |
| `SQL_ADMIN_PASSWORD` | SQL Server password | Every 90 days |
| `ACR_USERNAME` | Container registry | Rarely changes |
| `ACR_PASSWORD` | ACR password | Every 90 days |

### Local Development

- **Never** commit `.env` files
- Use `.env.example` as template (safe to commit)
- Store actual values in local `.env` (git-ignored)
- Use `terraform.tfvars` for local Terraform (git-ignored)
- Set sensitive Terraform vars via `TF_VAR_*` environment variables

### Azure Key Vault Integration

```csharp
// CORRECT: Use Key Vault via Managed Identity
if (configuration.GetValue<bool>("UseKeyVault"))
{
    var keyVaultUri = configuration["KeyVault:Uri"];
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

// WRONG: Hardcoded secrets
// var connectionString = "Server=...;Password=MyPassword123"; ❌
```

### Terraform State Security

- **Never** commit `terraform.tfstate` files
- Use remote backend (Azure Storage) - already configured
- State contains sensitive data (connection strings, keys)
- Ensure backend storage has access controls

---

## Pre-Commit Hooks

### Overview

Pre-commit hooks scan for secrets before allowing commits. Located at `.git/hooks/pre-commit`.

### What It Scans For

- Azure subscription IDs, tenant IDs, client IDs (UUID patterns)
- Connection strings and account keys
- Passwords, secrets, tokens, API keys in code
- Private SSH/RSA keys
- Access tokens (e.g., Bearer)
- Common secret files (`.env`, `secrets.json`, etc.)
- Hardcoded UUIDs in documentation files

### Installation

#### Windows (PowerShell)

Hook already installed at `.git/hooks/pre-commit.ps1`.

Ensure Git is configured to run PowerShell hooks:

```powershell
# Check current core.hooksPath
git config core.hooksPath

# If needed, reset to default
git config --unset core.hooksPath

# Test the hook
git commit --dry-run
```

#### Linux/Mac (Bash)

Make hook executable:

```bash
chmod +x .git/hooks/pre-commit
```

### Testing

Try committing a file with a fake secret (redacted example):

```bash
echo 'credential="REDACTED"' > test.txt
git add test.txt
git commit -m "Test"
# Should be blocked by pre-commit hook
```

Clean up:
```bash
git reset HEAD test.txt
rm test.txt
```

### Bypassing (Emergency Only)

⚠️ **NOT recommended:**

```bash
git commit --no-verify -m "Emergency commit"
```

Only use if you're certain no secrets are included.

### Troubleshooting

**Hook not running?**
- Check `.git/hooks/pre-commit` exists and is executable
- Verify you haven't set custom `core.hooksPath`
- On Windows, ensure PowerShell execution policy allows scripts

**False positives?**
- Review the pattern that triggered
- If legitimate, document why it's safe
- Consider refactoring to avoid secret-like patterns

---

## Application Security (.NET)

### Input Validation

**ALWAYS** use FluentValidation:

```csharp
public class UploadResumeCommandValidator : AbstractValidator<UploadResumeCommand>
{
    public UploadResumeCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .Must(BeValidFileType).WithMessage("Only PDF files are allowed")
            .Must(BeValidFileSize).WithMessage("File size must not exceed 10MB");
    }
}
```

### SQL Queries

```csharp
// CORRECT: Entity Framework with parameters
var resume = await _context.Resumes
    .Where(r => r.Id == id)
    .FirstOrDefaultAsync();

// WRONG: String concatenation
// var query = $"SELECT * FROM Resumes WHERE Id = {id}"; ❌
```

### Error Handling

```csharp
// CORRECT: Generic error in production
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing resume {ResumeId}", resumeId);
    
    return _environment.IsProduction() 
        ? new ProblemDetails { Title = "An error occurred processing your request" }
        : new ProblemDetails { Title = ex.Message, Detail = ex.StackTrace };
}

// WRONG: Exposing details in production
// return new ProblemDetails { Detail = ex.StackTrace }; ❌
```

### File Upload Security

**ALWAYS** validate:
1. File extension
2. File size
3. File content (magic numbers)
4. Scan for malware if possible

```csharp
private bool IsValidPdfFile(IFormFile file)
{
    // Check extension
    var extension = Path.GetExtension(file.FileName).ToLower();
    if (extension != ".pdf") return false;
    
    // Check size (10MB limit)
    if (file.Length > 10 * 1024 * 1024) return false;
    
    // Check magic numbers (PDF header)
    using var stream = file.OpenReadStream();
    var header = new byte[4];
    stream.Read(header, 0, 4);
    return header.SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF
}
```

### Authentication & Authorization

```csharp
// ALWAYS require authentication for sensitive endpoints
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ResumesController : ControllerBase
{
    // ALWAYS validate user owns the resource
    [HttpGet("{id}")]
    public async Task<IActionResult> GetResume(Guid id)
    {
        var resume = await _mediator.Send(new GetResumeByIdQuery(id));
        
        // Verify ownership
        if (resume.UserId != User.GetUserId())
            return Forbid();
            
        return Ok(resume);
    }
}
```

### CORS Configuration

```csharp
// CORRECT: Specific origins in production
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsProduction())
        {
            policy.WithOrigins("https://cvanalyzer.com")
                  .AllowCredentials();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// WRONG: AllowAnyOrigin in production ❌
```

### Logging Best Practices

**What to Log:**
- ✅ Authentication attempts (success/failure)
- ✅ Authorization failures
- ✅ File upload events
- ✅ Database errors
- ✅ External API calls
- ✅ Performance metrics

**What NOT to Log:**
- ❌ Passwords or tokens
- ❌ Credit card numbers
- ❌ PII without masking
- ❌ Full connection strings
- ❌ Session tokens

```csharp
// CORRECT: Structured logging with no sensitive data
_logger.LogInformation(
    "Resume uploaded successfully. ResumeId: {ResumeId}, UserId: {UserId}, FileSize: {FileSize}",
    resume.Id, userId, file.Length);

// WRONG: Logging sensitive data
// _logger.LogInformation($"User password: {password}"); ❌
```

---

## Infrastructure Security (Terraform/Azure)

### Azure Key Vault

- **Production:** MUST have purge protection enabled
- **Production:** MUST use network ACLs (default deny)
- **Dev/Test:** Can disable purge protection for easier testing
- **ALWAYS** store secrets in Key Vault, never in app settings

### Azure SQL Server

- **ALWAYS** enforce TLS 1.2 minimum
- **Production:** MUST disable public network access (use private endpoints)
- **ALWAYS** enable threat detection for production
- **NEVER** use common usernames (admin, sa, sqladmin)
- **ALWAYS** use strong, unique passwords per environment

### Azure Container Apps

- **ALWAYS** enforce HTTPS (`https_only = true`)
- **ALWAYS** set minimum TLS version to 1.2
- **ALWAYS** use managed identity for Azure resource access
- **Production:** SHOULD use private endpoints

### Resource Protection

- **Production:** MUST have `CanNotDelete` resource locks
- **ALWAYS** tag resources with Environment and Application
- **ALWAYS** use environment-specific naming: `{resource-type}-cvanalyzer-{env}`

### Terraform Code Rules

```terraform
# CORRECT: Use Azure CLI context
provider "azurerm" {
  # subscription_id inherited from: az account set
  features {}
}

# Strong, environment-specific username
sql_admin_username = "cvadmin_prod"

# Validated password
variable "sql_admin_password" {
  type      = string
  sensitive = true  # ALWAYS mark as sensitive
  
  validation {
    condition     = length(var.sql_admin_password) >= 12
    error_message = "Password must be at least 12 characters"
  }
}

# WRONG patterns:
# provider "azurerm" {
#   subscription_id = <YOUR_SUBSCRIPTION_ID> # ❌ Hardcoded (do not commit)
# }
# variable "password" {
#   type = string  # ❌ Missing sensitive = true
# }
```

### Best Practices

- **NEVER** hardcode subscription IDs, use Azure CLI context or env vars
- **ALWAYS** mark sensitive variables with `sensitive = true`
- **ALWAYS** validate input variables (environment, passwords, etc.)
- **ALWAYS** use separate `.tfvars` files per environment
- **NEVER** create circular dependencies between modules
- **ALWAYS** run `terraform fmt` and `terraform validate` before committing

---

## Security Checklist

### Before Every Commit

- [ ] Run `git status` and review all changed files
- [ ] Run `git diff --cached` to review exact changes
- [ ] Search for secrets: `git diff --cached | grep -i "password\|secret\|key"`
- [ ] Verify no `.env` or `*.tfvars` files staged (except `.example` files)
- [ ] Check that UUIDs in docs are redacted (use `xxxx...` instead of real IDs)
- [ ] Pre-commit hook passed (or understand why you're using `--no-verify`)

### Repository Configuration

**Completed:**
- ✅ `.gitignore` configured to exclude sensitive files
- ✅ Pre-commit hooks installed (`.git/hooks/pre-commit`)
- ✅ Secret scanning workflow configured
- ✅ Template files created (`.env.example`, `terraform.tfvars.example`)

**To Enable:**
- [ ] GitHub Secret Scanning - Push Protection (Settings > Security > Code security)
- [ ] Branch protection rules for `main` branch
- [ ] Required PR reviews before merging
- [ ] Status checks must pass before merge
- [ ] Dependabot alerts enabled
- [ ] Dependabot security updates enabled

### Regular Security Audits

**Weekly:**
- [ ] Review Dependabot alerts
- [ ] Check GitHub Security tab for vulnerabilities
- [ ] Review access logs for Container Apps

**Monthly:**
- [ ] Review and update dependencies
- [ ] Check for new Azure security recommendations
- [ ] Review IAM roles and permissions
- [ ] Audit GitHub repository access

**Quarterly:**
- [ ] Rotate all credentials
- [ ] Review and update security policies
- [ ] Conduct security training
- [ ] Test disaster recovery procedures

---

## Incident Response

### If Secrets Are Committed

**Immediate Actions:**

1. **DO NOT** force-push or delete history (makes it worse)
2. **Rotate compromised credentials immediately:**

```bash
# Rotate Azure service principal
az ad sp credential reset --id <client-id>

# Rotate SQL password
az sql server update --resource-group <rg> --name <server> --admin-password <new-password>

# Rotate ACR password
az acr credential renew --name <acr-name> --password-name password
```

3. **Update GitHub Secrets** with new credentials
4. **Report the incident** if required by company policy
5. **Review access logs** for unauthorized access

### Cleaning Git History (Advanced)

⚠️ **Only if absolutely necessary and coordinated with team:**

```bash
# Use BFG Repo-Cleaner
bfg --delete-files secrets.json
bfg --replace-text passwords.txt

# Or git-filter-repo
git filter-repo --path-glob '*.env' --invert-paths
```

**Warning:** This rewrites history. All team members must re-clone.

### Security Issue Discovered

If you discover a security vulnerability:

1. **DO NOT** commit directly to main branch
2. **DO NOT** create a public GitHub issue
3. **DO** create a security-specific branch
4. **DO** notify the security team immediately
5. **DO** rotate exposed credentials immediately
6. **DO** review git history for exposed secrets
7. **DO** document the issue and resolution

---

## Resources

### Tools

- **Pre-commit Hooks:** Local secret scanning (`.git/hooks/pre-commit`)
- **GitHub Actions:** TruffleHog + GitLeaks (`.github/workflows/security-scan.yml`)
- **Azure Key Vault:** Secret storage
- **Azure Managed Identity:** Passwordless authentication
- **Azure Security Center:** Security recommendations

### Security Validation Commands

```bash
# Check for secrets in Git
git log -p | grep -i "password\|secret\|key" | head -20

# Validate Terraform
terraform fmt -recursive
terraform validate
terraform plan -var-file="environments/prod.tfvars"

# Check .NET dependencies
dotnet list package --vulnerable
dotnet list package --outdated

# Run tests
dotnet test

# Check .gitignore
git status --ignored
```

### Documentation

- **DevOps Guide:** `docs/DEVOPS.md` - Deployment and CI/CD pipelines
- **Terraform Guide:** `docs/TERRAFORM.md` - Infrastructure as Code
- **Architecture Guide:** `docs/ARCHITECTURE.md` - System architecture
- **Copilot Instructions:** `.github/copilot-instructions.md` - AI coding guidelines

### External Resources

- [GitHub Security Best Practices](https://docs.github.com/en/code-security)
- [Azure Security Documentation](https://docs.microsoft.com/en-us/azure/security/)
- [OWASP Secrets Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)
- [Terraform Security Best Practices](https://www.terraform.io/docs/cloud/guides/recommended-practices/part1.html)

---

## Quick Start for New Team Members

1. [ ] Clone repository
2. [ ] Copy `.env.example` to `.env` and fill in values
3. [ ] Never commit `.env` file
4. [ ] Install pre-commit hooks (already in `.git/hooks/`)
5. [ ] Configure git: `git config --global user.email` and `git config --global user.name`
6. [ ] Read this security guide
7. [ ] Set up Azure CLI authentication: `az login`
8. [ ] Verify access to GitHub Secrets (if needed for deployment)
9. [ ] Test pre-commit hook: Try committing a file with a redacted token value (e.g., "credential=REDACTED")
10. [ ] Review other documentation in `docs/` folder

---

**Remember:** Security is not optional. When in doubt, ask! 🔒

For questions or concerns, contact the security team immediately.
