# CV Analyzer AI Service

Python-based AI service for resume analysis using Microsoft Agent Framework and Azure AI Foundry with GPT-4o.

## Features

- **Resume Analysis**: Comprehensive resume evaluation with scoring
- **AI Optimization**: GPT-4o powered content improvement
- **Structured Suggestions**: Categorized, prioritized improvement recommendations
- **FastAPI**: High-performance async API
- **Agent Framework**: Microsoft's Agent Framework for structured AI workflows

## Architecture

This service is part of the CV Analyzer microservices architecture:

```
.NET API (backend/) ──HTTP──> Python AI Service (ai-service/)
                                    │
                                    └──> Azure AI Foundry
                                             └──> GPT-4o Model
```

## Prerequisites

- Python 3.11+
- Azure AI Foundry project with GPT-4o deployment
- Azure credentials (Managed Identity or Service Principal)

## Installation

### Local Development

1. **Create virtual environment:**
   ```bash
   python -m venv venv
   source venv/bin/activate  # On Windows: venv\Scripts\activate
   ```

2. **Install dependencies:**
   ```bash
   pip install -r requirements.txt
   ```

3. **Set environment variables:**
   ```bash
   export AI_FOUNDRY_ENDPOINT="https://your-project.api.azureml.ms"
   export MODEL_DEPLOYMENT_NAME="gpt-4o-deployment"
   export AZURE_CLIENT_ID="your-client-id"  # If using Service Principal
   export AZURE_TENANT_ID="your-tenant-id"
   export AZURE_CLIENT_SECRET="your-secret"
   ```

4. **Run the service:**
   ```bash
   python -m app.main
   # or
   uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
   ```

### Docker

1. **Build image:**
   ```bash
   docker build -t cv-analyzer-ai-service .
   ```

2. **Run container:**
   ```bash
   docker run -p 8000:8000 \
     -e AI_FOUNDRY_ENDPOINT="https://your-project.api.azureml.ms" \
     -e MODEL_DEPLOYMENT_NAME="gpt-4o-deployment" \
     cv-analyzer-ai-service
   ```

## API Endpoints

### Health Check
```http
GET /health
```

**Response:**
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "ai_foundry_connected": true
}
```

### Analyze Resume
```http
POST /analyze
Content-Type: application/json

{
  "content": "Software Engineer with 5 years of experience...",
  "user_id": "user123"
}
```

**Response:**
```json
{
  "score": 85.5,
  "optimized_content": "Senior Software Engineer with 5+ years...",
  "suggestions": [
    {
      "category": "Skills",
      "description": "Add cloud platform experience (Azure, AWS)",
      "priority": 1
    }
  ],
  "analysis_metadata": {
    "processing_time_ms": 1234.56,
    "model_used": "gpt-4o-deployment",
    "timestamp": "2025-11-04T12:00:00Z"
  }
}
```

## Configuration

Environment variables:

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `AI_FOUNDRY_ENDPOINT` | Azure AI Foundry project endpoint | - | ✅ |
| `MODEL_DEPLOYMENT_NAME` | GPT-4o deployment name | `gpt-4o-deployment` | ✅ |
| `LOG_LEVEL` | Logging level | `INFO` | ❌ |
| `AZURE_CLIENT_ID` | Service Principal Client ID | - | ❌* |
| `AZURE_TENANT_ID` | Azure Tenant ID | - | ❌* |
| `AZURE_CLIENT_SECRET` | Service Principal Secret | - | ❌* |

*Required if not using Managed Identity

## Development

### Project Structure

```
ai-service/
├── app/
│   ├── __init__.py       # Package initialization
│   ├── main.py           # FastAPI application
│   ├── agent.py          # Agent Framework logic
│   ├── models.py         # Pydantic models
│   └── config.py         # Configuration
├── requirements.txt      # Python dependencies
├── Dockerfile           # Container definition
└── README.md            # This file
```

### Testing

```bash
# Run with test data
curl -X POST http://localhost:8000/analyze \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Software Engineer with experience in Python, C#, and cloud technologies...",
    "user_id": "test-user"
  }'
```

### API Documentation

Interactive API documentation available at:
- Swagger UI: http://localhost:8000/docs
- ReDoc: http://localhost:8000/redoc

## Integration with .NET Backend

The .NET API calls this service via HTTP:

```csharp
// In CVAnalyzer.Infrastructure/Services/AIResumeAnalyzerService.cs
var response = await _httpClient.PostAsJsonAsync("/analyze", new
{
    content = resumeContent,
    user_id = userId
});

var analysis = await response.Content.ReadFromJsonAsync<ResumeAnalysisResponse>();
```

## Security

- **Authentication**: Uses Azure Managed Identity or Service Principal
- **No API Keys**: Credentials managed by Azure
- **Input Validation**: Pydantic models validate all inputs
- **Error Handling**: Sensitive information not exposed in errors
- **CORS**: Configure allowed origins for production

## Performance

- **Async Processing**: Non-blocking I/O for high throughput
- **Connection Pooling**: Efficient Azure AI Foundry connections
- **Timeouts**: Configurable request timeouts (default: 30s)
- **Token Limits**: Max 10,000 characters per analysis

## Monitoring

Logs include:
- Request/response times
- User IDs (for tracking)
- Error details
- AI model responses

Example log:
```
2025-11-04 12:00:00 - app.agent - INFO - Analyzing resume for user test-user, length: 1234 chars
2025-11-04 12:00:01 - app.agent - INFO - Analysis complete for user test-user. Score: 85.5, Time: 1234.56ms
```

## Troubleshooting

### Common Issues

**1. "Agent not initialized"**
- Ensure `AI_FOUNDRY_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` are set
- Check Azure credentials are valid
- Verify network connectivity to Azure AI Foundry

**2. "Analysis request timed out"**
- Increase `request_timeout` in config
- Check resume content length (max 10,000 chars)
- Verify GPT-4o deployment has sufficient capacity

**3. Import errors**
- Install dependencies: `pip install -r requirements.txt`
- Use `--pre` flag for Agent Framework: `pip install agent-framework-azure-ai --pre`

## License

MIT License - Part of CV Analyzer project
