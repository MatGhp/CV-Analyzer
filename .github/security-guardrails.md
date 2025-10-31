# Security & Guardrails - CV Analyzer Project

## Purpose
This file provides security guardrails and best practices for AI coding agents working on the CV Analyzer project. Follow these rules strictly to maintain security posture and prevent vulnerabilities.

---

## 🔴 CRITICAL - Never Do These

### 1. Secrets & Credentials
- **NEVER** commit passwords, API keys, connection strings, or tokens to Git
- **NEVER** hardcode subscription IDs, tenant IDs, or client secrets in code
- **NEVER** log sensitive data (passwords, PII, connection strings)
- **NEVER** include secrets in exception messages or error responses
- **NEVER** commit `.tfvars` files with real values (only `.tfvars.example` allowed)

### 2. SQL Injection Prevention
- **NEVER** concatenate user input directly into SQL queries
- **ALWAYS** use parameterized queries via Entity Framework
- **NEVER** use `FromSqlRaw` with unsanitized input
- **ALWAYS** validate and sanitize file uploads before processing

### 3. Authentication & Authorization
- **NEVER** bypass authentication checks
- **NEVER** expose endpoints without proper authorization
- **NEVER** use weak password requirements (min 12 chars, complexity required)
- **NEVER** store passwords in plain text
- **NEVER** disable HTTPS in production

### 4. Data Protection
- **NEVER** return sensitive data in API responses without masking
- **NEVER** expose stack traces or detailed errors to clients in production
- **NEVER** disable encryption for data in transit or at rest
- **NEVER** allow unrestricted file uploads (validate type, size, content)

---

## 🟠 Infrastructure Security Rules (Terraform)

### Azure Resources
1. **Key Vault**
   - Production: MUST have purge protection enabled
   - Production: MUST use network ACLs (default deny)
   - Dev/Test: Can disable purge protection for easier testing
   - ALWAYS store secrets in Key Vault, never in app settings

2. **SQL Server**
   - ALWAYS enforce TLS 1.2 minimum
   - Production: MUST disable public network access (use private endpoints)
   - ALWAYS enable threat detection for production
   - NEVER use common usernames (admin, sa, sqladmin)
   - ALWAYS use strong, unique passwords per environment

3. **App Service**
   - ALWAYS enforce HTTPS (`https_only = true`)
   - ALWAYS set minimum TLS version to 1.2
   - ALWAYS disable basic authentication for FTP and WebDeploy
   - ALWAYS use managed identity for Azure resource access
   - Production: SHOULD use private endpoints

4. **Resource Protection**
   - Production: MUST have `CanNotDelete` resource locks
   - ALWAYS tag resources with Environment and Application
   - ALWAYS use environment-specific naming: `{resource-type}-cvanalyzer-{env}`

### Terraform Code Rules
- **NEVER** hardcode subscription IDs, use Azure CLI context or env vars
- **ALWAYS** mark sensitive variables with `sensitive = true`
- **ALWAYS** validate input variables (environment, passwords, etc.)
- **ALWAYS** use separate `.tfvars` files per environment
- **NEVER** create circular dependencies between modules
- **ALWAYS** run `terraform fmt` and `terraform validate` before committing

---

## 🟡 Application Security Rules (.NET)

### Input Validation
```csharp
// ALWAYS validate input using FluentValidation
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

### Secrets Management
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
```csharp
// ALWAYS validate:
// 1. File extension
// 2. File size
// 3. File content (magic numbers)
// 4. Scan for malware if possible

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

---

## 🔵 API Security Best Practices

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

### Rate Limiting (TODO)
```csharp
// SHOULD implement rate limiting for public endpoints
// [RateLimit(RequestsPerMinute = 60)]
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

---

## 🟢 Logging & Monitoring Rules

### What to Log
✅ Authentication attempts (success/failure)
✅ Authorization failures
✅ File upload events
✅ Database errors
✅ External API calls
✅ Performance metrics

### What NOT to Log
❌ Passwords or tokens
❌ Credit card numbers
❌ Personal Identifiable Information (PII) without masking
❌ Full connection strings
❌ Session tokens

### Example
```csharp
// CORRECT: Structured logging with no sensitive data
_logger.LogInformation(
    "Resume uploaded successfully. ResumeId: {ResumeId}, UserId: {UserId}, FileSize: {FileSize}",
    resume.Id, userId, file.Length);

// WRONG: Logging sensitive data
// _logger.LogInformation($"User password: {password}"); ❌
```

---

## 📋 Code Review Checklist

Before committing code, verify:

### Security
- [ ] No hardcoded secrets or credentials
- [ ] All user input is validated
- [ ] SQL queries use parameterization
- [ ] File uploads are validated (type, size, content)
- [ ] Authentication/authorization is properly implemented
- [ ] Sensitive data is not logged
- [ ] Error messages don't expose internal details in production

### Infrastructure
- [ ] No hardcoded subscription IDs in Terraform
- [ ] Sensitive Terraform variables marked as `sensitive = true`
- [ ] Production resources have appropriate security controls
- [ ] Resource locks enabled for production
- [ ] `.tfvars` files not committed (only examples)

### Quality
- [ ] Code follows KISS principle
- [ ] FluentValidation used for input validation
- [ ] MediatR handlers are single-purpose
- [ ] Exception handling middleware catches all errors
- [ ] Logging is structured and meaningful

---

## 🚨 Incident Response

If a security issue is discovered:

1. **DO NOT** commit the fix directly to main branch
2. **DO** create a security-specific branch
3. **DO** notify the security team immediately
4. **DO** rotate any exposed credentials immediately
5. **DO** review git history for exposed secrets
6. **DO** update this document if new patterns emerge

### Tools for Secret Scanning
- GitHub Secret Scanning (enabled)
- git-secrets (local pre-commit hook)
- TruffleHog (CI/CD pipeline)

---

## 🔐 Environment-Specific Rules

### Development
- Relaxed security for ease of testing
- Local secrets can use User Secrets (`dotnet user-secrets`)
- Detailed error messages allowed
- SQL public access allowed

### Test
- Moderate security
- Use Key Vault for secrets
- Generic error messages
- SQL public access allowed with firewall rules

### Production
- **MAXIMUM** security
- ALL secrets in Key Vault
- Generic error messages only
- SQL public access DISABLED (private endpoints)
- Purge protection ENABLED
- Resource locks ENABLED
- Threat detection ENABLED
- Diagnostic logging ENABLED

---

## 📚 Reference Documentation

- Security Review: `SECURITY_REVIEW.md`
- Terraform Best Practices: `.github/terraform-instructions.md`
- Application Architecture: `.github/copilot-instructions.md`
- Code Review Summary: `CODE_REVIEW_SUMMARY.md`

---

## 🎯 Quick Security Commands

### Check for secrets in Git history
```bash
git log -p | grep -i password
git log -p | grep -i secret
```

### Validate Terraform security
```bash
terraform validate
terraform plan -var-file="environments/prod.tfvars"
# Review all security settings before apply
```

### Check .NET dependencies for vulnerabilities
```bash
dotnet list package --vulnerable
dotnet list package --outdated
```

### Test SQL injection prevention
```bash
# Use OWASP ZAP or similar tools
# Test with: ' OR '1'='1
# Should be rejected by FluentValidation
```

---

## ✅ Security Compliance

This project aims to comply with:

- **GDPR**: Data protection, encryption, access controls
- **ISO 27001**: Security management, monitoring, documentation
- **SOC 2**: Access management, encryption, audit logging
- **OWASP Top 10**: Protection against common vulnerabilities

---

**Last Updated**: October 31, 2025  
**Version**: 1.0  
**Owner**: CV Analyzer Security Team

**Remember**: Security is everyone's responsibility. When in doubt, ask!
