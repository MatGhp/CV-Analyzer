# Security Checklist - CV Analyzer

## 🔒 Before Every Commit

- [ ] Run `git status` and review all changed files
- [ ] Run `git diff --cached` to review exact changes
- [ ] Search for accidentally included secrets: `git diff --cached | grep -i "password\|secret\|key"`
- [ ] Verify no `.env` or `*.tfvars` files are staged (except `.example` files)
- [ ] Check that UUIDs in docs are redacted (use `xxxx...` instead of real IDs)
- [ ] Pre-commit hook passed (or understand why you're using `--no-verify`)

## 🛡️ Repository Configuration

### Completed ✅
- [x] `.gitignore` configured to exclude sensitive files
- [x] Pre-commit hooks installed (`.git/hooks/pre-commit`)
- [x] Secret scanning workflow configured
- [x] Template files created (`.env.example`, `terraform.tfvars.example`)
- [x] GitHub Advanced Security enabled (see repository settings)

### To Enable
- [ ] GitHub Secret Scanning - Push Protection (Settings > Security > Code security)
- [ ] Branch protection rules for `main` branch
- [ ] Required PR reviews before merging
- [ ] Status checks must pass before merge
- [ ] Dependabot alerts enabled
- [ ] Dependabot security updates enabled

## 🔐 Secret Management Best Practices

### GitHub Secrets (Repository Settings > Secrets)
All sensitive values should be stored as GitHub Secrets:

| Secret Name | Purpose | Rotation |
|-------------|---------|----------|
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
- Use `.env.example` as template
- Store actual values in local `.env` (git-ignored)
- Use `terraform.tfvars` for local Terraform (git-ignored)
- Set sensitive Terraform vars via `TF_VAR_*` environment variables

### Terraform State
- **Never** commit `terraform.tfstate` files
- Use remote backend (Azure Storage) - already configured
- State contains sensitive data (connection strings, keys)
- Ensure backend storage has access controls

## 🚨 If Secrets Are Committed

### Immediate Actions
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
Only if absolutely necessary and coordinated with team:
```bash
# Use BFG Repo-Cleaner
bfg --delete-files secrets.json
bfg --replace-text passwords.txt

# Or git-filter-repo
git filter-repo --path-glob '*.env' --invert-paths
```

⚠️ **Warning:** This rewrites history. All team members must re-clone.

## 🔍 Regular Security Audits

### Weekly
- [ ] Review Dependabot alerts
- [ ] Check GitHub Security tab for vulnerabilities
- [ ] Review access logs for Container Apps

### Monthly
- [ ] Review and update dependencies
- [ ] Check for new Azure security recommendations
- [ ] Review IAM roles and permissions
- [ ] Audit GitHub repository access

### Quarterly
- [ ] Rotate all credentials
- [ ] Review and update security policies
- [ ] Conduct security training
- [ ] Test disaster recovery procedures

## 🎯 Secret Detection Tools

### Pre-commit Hooks (Local)
Located in `.git/hooks/pre-commit`:
- Scans for common secret patterns
- Blocks commits with potential secrets
- Bypass: `git commit --no-verify` (NOT recommended)

### GitHub Actions (CI/CD)
Workflow: `.github/workflows/security-scan.yml`
- TruffleHog - Comprehensive secret scanning
- GitLeaks - Fast and configurable scanner
- Custom checks for sensitive files

### Azure Tools
- Azure Key Vault for secret storage
- Azure Managed Identity for passwordless auth
- Azure Security Center for recommendations

## 📚 Additional Resources

- [GitHub Security Best Practices](https://docs.github.com/en/code-security)
- [Azure Security Documentation](https://docs.microsoft.com/en-us/azure/security/)
- [OWASP Secrets Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)
- [Terraform Security Best Practices](https://www.terraform.io/docs/cloud/guides/recommended-practices/part1.html)

## ✅ Quick Start Checklist

For new team members:

1. [ ] Clone repository
2. [ ] Copy `.env.example` to `.env` and fill in values
3. [ ] Never commit `.env` file
4. [ ] Install pre-commit hooks (already in `.git/hooks/`)
5. [ ] Configure git: `git config --global user.email` and `git config --global user.name`
6. [ ] Read this security checklist
7. [ ] Review `.github/security-guardrails.md`
8. [ ] Set up Azure CLI authentication: `az login`
9. [ ] Verify access to GitHub Secrets (if needed for deployment)
10. [ ] Test pre-commit hook: Try committing a file with "password=secret123"

## 🆘 Support

If you discover a security vulnerability:
1. **DO NOT** create a public GitHub issue
2. Contact the security team immediately
3. Document what you found
4. Follow responsible disclosure guidelines

---

**Last Updated:** November 5, 2025
**Next Review:** February 5, 2026
