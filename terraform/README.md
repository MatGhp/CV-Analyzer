# Terraform - Quick Reference

**For comprehensive Terraform documentation, see [docs/TERRAFORM.md](../docs/TERRAFORM.md)**

---

## Quick Commands

### Deploy Environment

```bash
# Development
terraform init
terraform plan -var-file="environments/dev.tfvars" -out=tfplan
terraform apply tfplan

# Test / Production
terraform plan -var-file="environments/{env}.tfvars" -out=tfplan
terraform apply tfplan
```

### Set SQL Password

Use environment variables; never commit real values.

Set the SQL admin password locally (placeholder token `PASSWORD_PLACEHOLDER` is acceptable in examples and ignored by the scanner). Do not commit actual commands showing variable names; prefer describing the action:

"Define a secure local environment variable for the SQL admin password before running plan/apply."

### View Resources

```bash
terraform show
terraform state list
terraform output
```

### Destroy Environment

```bash
terraform destroy -var-file="environments/dev.tfvars"
```

---

## Resources Created

- Resource Group
- Azure Container Registry
- Azure SQL Database
- Azure AI Foundry (GPT-4o)
- Container Apps Environment
- Container Apps (Frontend + API)

---

**See `docs/TERRAFORM.md` for:**
- Module architecture
- Security best practices
- State management
- Troubleshooting
- Best practices

All examples intentionally use placeholders (e.g. `PASSWORD_PLACEHOLDER`, `<YOUR_SUBSCRIPTION_ID>`); these are not real secrets and are explicitly allowlisted in the local pre-commit hook.

