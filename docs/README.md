# CV Analyzer Documentation Index

Your starting point for all project docs. Each guide is concise, actionable, and aligned with our current architecture (Angular 20 + .NET 9 + Microsoft Agent Framework + Azure OpenAI).

## Core Guides

- **🎯 Implementation Status**: `docs/IMPLEMENTATION_STATUS.md` — What's working, verified tests, lessons learned **[START HERE]**
- **Architecture**: `docs/ARCHITECTURE.md` — System, components, data flow, and background processing ⚡
- **Security**: `docs/SECURITY.md` — Guardrails, secret management, hooks, checklists
- **Running Locally**: `../RUNNING_LOCALLY.md` — Local dev setup with Azure resources 💻
- **Quick Start**: `../QUICKSTART.md` — Get started in 5 minutes
- **DevOps**: `docs/DEVOPS.md` — CI/CD, environments, operations
- **Terraform**: `docs/TERRAFORM.md` — Infrastructure as Code, modules, workflows
- **Git Workflow**: `docs/GIT_WORKFLOW.md` — Branch naming, commits, rebase, and secret handling
- **Frontend MVP**: `docs/FRONTEND_MVP.md` — Angular UI components and feature requirements

## Future Architecture

- **Golden Architecture**: `docs/GOLDEN_ARCHITECTURE.md` — Microsoft's enterprise agentic architecture roadmap
- **Agent Framework**: `docs/AGENT_FRAMEWORK.md` — Migration path to Microsoft Agent Framework
- **Durable Agents Roadmap**: `docs/DURABLE_AGENTS_ROADMAP.md` — Upgrade to stateful multi-agent orchestrations (3-4 week plan)
- **Refactoring Summary**: `docs/REFACTORING_SUMMARY.md` — KISS improvements & rationale

## Documentation Quality

- **Documentation Review Summary**: `docs/DOCUMENTATION_REVIEW_SUMMARY.md` — Expert review results (Nov 16, 2025) - Quality score 98/100

## Quick Starts

- Local stack with Docker Compose: see root `README.md` Quick Start
- Backend (.NET) development: `backend/README.md`
- Frontend (Angular) development: `frontend/README.md`

## Common Issues & Troubleshooting

### Azure OpenAI Returns Empty Response

**Symptom**: Logs show "Received completion with 1 choices" but "Agent response was empty"

**Cause**: Using deprecated `Functions` API — Azure OpenAI returns function call arguments instead of text content.

**Solution**: Check `message.FunctionCall.Arguments` instead of `message.Content`

**Code Pattern**:
```csharp
string jsonPayload;
if (message.FunctionCall != null) {
    jsonPayload = message.FunctionCall.Arguments;  // ✓ Correct
} else {
    jsonPayload = message.Content;  // Fallback for non-function responses
}
```

### Azure OpenAI DNS Failure

**Symptom**: `SocketException: No such host is known` for `ai-cvanalyzer-dev.openai.azure.com`

**Cause**: Incorrect endpoint format. Multi-service AI accounts use regional endpoints.

**Solution**: Use `https://swedencentral.api.cognitive.microsoft.com/` (not resource-specific hostname)

**Verify Endpoint**:
```powershell
az cognitiveservices account show --name ai-cvanalyzer-dev --resource-group rg-cvanalyzer-dev --query properties.endpoints
```

### DefaultAzureCredential Fails Locally

**Symptom**: "DefaultAzureCredential failed to retrieve a token"

**Cause**: No managed identity or Azure CLI login available locally

**Solutions**:
1. **Preferred**: Run `az login` before starting backend
2. **Alternative**: Add `ApiKey` to `AgentServiceOptions` for local dev

```csharp
if (!string.IsNullOrEmpty(options.ApiKey)) {
    return new OpenAIClient(endpoint, new AzureKeyCredential(options.ApiKey));
}
return new OpenAIClient(endpoint, new DefaultAzureCredential());
```

### Backend Stops After Processing Message

**Symptom**: BackgroundService logs "ResumeAnalysisWorker stopped" after processing one message

**Cause**: Expected behavior — worker runs for 3 minutes then stops if no messages

**Solution**: This is normal. Worker checks queue every 30 seconds for 6 iterations (3 minutes total), then gracefully stops. Restart backend to process new messages or keep it running continuously in production.

## Contributing

- Read `docs/SECURITY.md` before any code change
- Follow `.github/copilot-instructions.md` for coding with Copilot
- Update docs when changing public behavior or infrastructure

---

If something is missing or unclear, open a PR to update these docs.
