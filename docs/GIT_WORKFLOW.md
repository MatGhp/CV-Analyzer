# Git Workflow – CV Analyzer Monorepo

**Audience**: All developers contributing to CV Analyzer

**Key Principles**:
- Short-lived feature branches off `main`
- Conventional Commits format
- Daily rebasing to avoid merge conflicts
- Never commit secrets (pre-commit hook enforced)

This guide streamlines daily Git usage across Angular frontend, .NET backend, AgentService, and Terraform infrastructure.

---

---
## 1. Branching Strategy

Use short-lived feature branches off `main`:

```
git switch -c feat/frontend-upload
git switch -c fix/backend-null-ref
git switch -c chore/terraform-cleanup
```

Prefix branch names with a category (`feat/`, `fix/`, `chore/`, `refactor/`, `docs/`). Keep names concise and scoped.

Avoid long-running branches; rebase daily to reduce merge friction:
```
git fetch origin
git rebase origin/main
```

If conflicts arise, resolve, then continue:
```
git rebase --continue
```

---
## 2. Commit Hygiene

Follow Conventional Commits:
```
feat(frontend): add drag-and-drop resume upload
fix(ai-service): handle empty suggestion list parsing
chore(terraform): remove obsolete state backups
```

Atomic commits: one logical change (UI component, backend handler, doc update). Large refactors: split into mechanical (rename/move) then behavioral changes.

Use the commit template:
```
git config commit.template .gitmessage.txt
git commit
```

Amend last commit before pushing if only local adjustments:
```
git commit --amend --no-edit
```

Never amend public commits already pushed unless coordinating (force push risks CI audits).

---
## 3. Reviewing Staged Changes

Selective staging prevents accidental secret or unrelated file inclusion:
```
git add -p              # interactive hunks
git restore --staged <file>  # unstage
git diff --cached       # review staged diff
```

Use path filters for monorepo scope:
```
git add frontend/src/app/features/upload/
git add backend/src/CVAnalyzer.Application/Features/Resumes/
```

---
## 4. Secret Scanning & Hooks

Pre-commit hook scans only added lines and ignores approved placeholders like `<PASSWORD_PLACEHOLDER>`. If blocked:
1. Ensure removed lines aren’t being flagged by a secondary hook or tool.
2. Rewrite sensitive examples into placeholder tokens.
3. Re-run: `git diff --cached | grep -i password` (should show none except placeholders).

Bypass (last resort):
```
git commit --no-verify
```
Only when absolutely certain no real secrets remain—CI will still scan.

---
## 5. Keeping Terraform Clean

Never stage `terraform.tfstate*`, `.terraform/`, or plan files. If they appear:
```
git restore --staged terraform/terraform.tfstate
git clean -fd terraform/.terraform
```

Generate plans locally; optionally save a redacted summary (`terraform show -no-color tfplan > plan.txt`) if review needed—do not commit raw plan.

---
## 6. Rebasing vs Merging

Prefer rebasing feature branches for a linear history; merge only for large integration branches.

Rebase workflow:
```
git fetch origin
git rebase origin/main
git push --force-with-lease   # safe force push
```

`--force-with-lease` protects against overwriting others’ recent updates.

---
## 7. Handling Large Changes

For cross-service changes (e.g., new resume scoring field):
1. Backend domain & application updates + tests.
2. AI service model & response adaptation.
3. Frontend model and component updates.
4. Terraform variable or secret wiring (if needed).
Commit sequentially with clear scopes:
```
feat(domain): add ScoreExplanation to Resume
feat(ai-service): include explanation in analysis response
feat(frontend): display score explanation in results card
```

---
## 8. Diffs & Tools

Use word-level and function-level diff for clarity:
```
git diff --word-diff
git log -p --follow backend/src/CVAnalyzer.Domain/Entities/Resume.cs
```

Search renames across layers:
```
git grep -n ScoreExplanation
```

---
## 9. Tagging & Releases

Create annotated tags for release candidates and production:
```
git tag -a v0.2.0 -m "Release v0.2.0"
git push origin v0.2.0
```

Changelog generation (manual for now): filter conventional commits since last tag:
```
git log v0.1.0..v0.2.0 --pretty=format:"%s" | grep -E "^(feat|fix|perf)"
```

---
## 10. Useful Aliases (see scripts/git-aliases.ps1)

Add to global config:
```
git config --global alias.st "status -sb"
git config --global alias.br "branch -vv"
git config --global alias.lg "log --oneline --graph --decorate"
git config --global alias.unstage "restore --staged"
```

---
## 11. Troubleshooting

Detached HEAD after checkout of tag:
```
git switch -c hotfix/tag-v0.2.0
```

Accidental commit with unwanted files:
```
git reset HEAD~1   # then re-stage selectively
```

Undo staged but not committed secret:
```
git restore --staged path/to/file
vim path/to/file   # redact
git add path/to/file
```

---
## 12. CI Interaction

Push triggers:
- Build/Test workflow
- Secret scan
- (When applicable) Infrastructure plan/deploy

If CI fails on secrets while local hook passed: the remote scanner checks full repo & history. Use history search:
```
git log -p | grep -i "password\|secret\|key" | head -50
```
Remove via history rewrite only if necessary (coordinate with team).

---
## 13. Next Enhancements (Future Work)

- Introduce automated changelog generation.
- Add commit linting GitHub Action.
- Pre-push hook to run fast test subset.
- Git LFS evaluation for large binary assets (none currently required).

---
## 14. Quick Checklist Before Push

- [ ] Branch name clear & scoped
- [ ] Conventional commits
- [ ] No secrets (placeholders only)
- [ ] Tests updated / green locally
- [ ] Terraform state not staged
- [ ] Rebased on latest `main`

---
Happy committing! Keep history clean, secure, and readable.
