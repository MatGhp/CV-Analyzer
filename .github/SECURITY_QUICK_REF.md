# 🔐 Security Quick Reference - CV Analyzer

**Last Updated:** October 31, 2025

---

## ⚡ Critical Security Rules (Never Break These!)

### 🔴 NEVER
- ❌ Commit secrets/passwords/API keys to Git
- ❌ Hardcode subscription IDs or tenant IDs
- ❌ Concatenate user input into SQL queries
- ❌ Expose stack traces in production
- ❌ Disable HTTPS in production
- ❌ Use weak passwords (min 12 chars required)
- ❌ Log sensitive data (passwords, PII, tokens)
- ❌ Bypass authentication or authorization
- ❌ Commit `.tfvars` files with real values

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

---

## 🛡️ Environment-Specific Security

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

---

## 📋 Pre-Commit Checklist

Before committing code, ask yourself:

- [ ] Are there any hardcoded secrets? (Search for: password, secret, key, token)
- [ ] Are all `.tfvars` files in `.gitignore`?
- [ ] Is user input validated with FluentValidation?
- [ ] Are SQL queries using Entity Framework (no string concatenation)?
- [ ] Are sensitive Terraform variables marked `sensitive = true`?
- [ ] Did I run `terraform fmt` and `terraform validate`?
- [ ] Are production resources properly secured?
- [ ] Is logging free of sensitive data?

---

## 🚨 Common Security Mistakes

### ❌ WRONG
```csharp
// Hardcoded connection string
var conn = "Server=sql.azure.com;Password=MyPass123";

// SQL injection risk
var query = $"SELECT * FROM Users WHERE Id = {userId}";

// Exposing sensitive data in logs
_logger.LogInformation($"User password: {password}");

// No input validation
public async Task<IActionResult> Upload(IFormFile file)
{
    // Direct processing without validation
}
```

```terraform
# Hardcoded subscription
provider "azurerm" {
  subscription_id = "12345678-1234-1234-1234-123456789abc"
}

# Weak SQL username
sql_admin_username = "admin"

# No password validation
variable "sql_admin_password" {
  type = string
  # Missing: validation, sensitive = true
}
```

### ✅ CORRECT
```csharp
// Use Key Vault
builder.Configuration.AddAzureKeyVault(
    new Uri(keyVaultUri),
    new DefaultAzureCredential());

// Use Entity Framework
var user = await _context.Users
    .Where(u => u.Id == userId)
    .FirstOrDefaultAsync();

// Safe logging
_logger.LogInformation("User authenticated. UserId: {UserId}", userId);

// Proper validation
public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .Must(BeValidFileType)
            .Must(BeValidFileSize);
    }
}
```

```terraform
# Use Azure CLI context
provider "azurerm" {
  # subscription_id inherited from: az account set
  features {}
}

# Strong, environment-specific username
sql_admin_username = "cvadmin_prod"

# Validated password
variable "sql_admin_password" {
  type      = string
  sensitive = true
  
  validation {
    condition     = length(var.sql_admin_password) >= 12
    error_message = "Password must be at least 12 characters"
  }
}
```

---

## 🔍 Security Validation Commands

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

---

## 📚 Full Documentation

- **Complete Security Guide:** `.github/security-guardrails.md`
- **Security Review:** `SECURITY_REVIEW.md`
- **Code Review Summary:** `CODE_REVIEW_SUMMARY.md`
- **Terraform Best Practices:** `.github/terraform-instructions.md`
- **Architecture Guide:** `.github/copilot-instructions.md`

---

## 🆘 Need Help?

**Security Issue Found?**
1. DO NOT commit to main branch
2. Create security-specific branch
3. Rotate exposed credentials immediately
4. Notify security team

**Questions?**
- Review the full security guardrails document
- Check recent security review results
- Consult team security lead

---

**Remember:** Security is not optional. When in doubt, ask! 🔒
