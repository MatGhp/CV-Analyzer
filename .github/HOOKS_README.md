# Git Hooks Installation Guide

## Pre-commit Hook for Secret Scanning

This repository includes a pre-commit hook that scans for secrets before allowing commits.

### Installation

#### Windows (PowerShell)

The PowerShell hook is already in place at `.git/hooks/pre-commit.ps1`.

To enable it, ensure your Git is configured to run PowerShell hooks:

```powershell
# Check current core.hooksPath
git config core.hooksPath

# If needed, reset to default
git config --unset core.hooksPath

# Test the hook
git commit --dry-run
```

#### Linux/Mac (Bash)

The bash hook is at `.git/hooks/pre-commit`.

Make it executable:

```bash
chmod +x .git/hooks/pre-commit
```

### What It Scans For

- Azure subscription IDs, tenant IDs, client IDs (UUID patterns)
- Connection strings and account keys
- Passwords, secrets, tokens, API keys in code
- Private SSH/RSA keys
- Bearer tokens
- Common secret files (.env, secrets.json, etc.)
- Hardcoded UUIDs in documentation files

### Testing the Hook

Try committing a file with a fake secret to test:

```bash
echo 'password="secret123"' > test.txt
git add test.txt
git commit -m "Test"
# Should be blocked by pre-commit hook
```

Clean up:
```bash
git reset HEAD test.txt
rm test.txt
```

### Bypassing the Hook (Emergency Only)

If you absolutely must bypass (NOT recommended):

```bash
git commit --no-verify -m "Emergency commit"
```

**Warning:** Only use this if you're certain no secrets are included.

### Troubleshooting

**Hook not running?**
- Check `.git/hooks/pre-commit` exists and is executable
- Verify you haven't set a custom `core.hooksPath`
- On Windows, ensure PowerShell execution policy allows scripts

**False positives?**
- Review the pattern that triggered
- If legitimate, document why it's safe
- Consider refactoring to avoid patterns that look like secrets

**Hook fails with error?**
- Check you have `git`, `grep`, and PowerShell/bash installed
- Verify file permissions
- Review the hook script for syntax errors

## GitHub Actions Secret Scanning

In addition to local hooks, GitHub Actions runs comprehensive secret scanning on every push and PR.

See `.github/workflows/security-scan.yml` for details.

## Support

If you have questions about the hooks or secret scanning, check:
- `.github/SECURITY_CHECKLIST.md` - Complete security checklist
- `.github/security-guardrails.md` - Security best practices
- `.github/DEPLOYMENT.md` - Deployment guide (with redacted examples)
