git config core.hooksPath
git config --unset core.hooksPath
git commit --dry-run
echo 'password="REDACTED"' > test.txt  # example only; do not use real secrets
git add test.txt
git commit -m "Test"
git reset HEAD test.txt
git commit --no-verify -m "Emergency commit"
# Git Hooks (Deprecated standalone doc)

This content is now consolidated into the main security guide: `docs/SECURITY.md` (see the Pre-Commit Hooks section).

Use that document for:
- Installation & testing
- What patterns are scanned
- Troubleshooting & false positives
- Bypassing in emergencies (discouraged)

Related workflows:
- GitHub Action secret scanning: `.github/workflows/security-scan.yml`

This file remains only as a pointer to prevent confusion.
