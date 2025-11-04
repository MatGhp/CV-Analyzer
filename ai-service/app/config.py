"""
Configuration for AI Service
"""
import os
from pydantic_settings import BaseSettings
from functools import lru_cache


class Settings(BaseSettings):
    """Application settings loaded from environment variables"""
    
    # Azure AI Foundry
    ai_foundry_endpoint: str = os.getenv(
        "AI_FOUNDRY_ENDPOINT", 
        "https://your-foundry-project.api.azureml.ms"
    )
    model_deployment_name: str = os.getenv(
        "MODEL_DEPLOYMENT_NAME", 
        "gpt-4o-deployment"
    )
    
    # Service Configuration
    service_name: str = "cv-analyzer-ai-service"
    log_level: str = os.getenv("LOG_LEVEL", "INFO")
    
    # API Configuration
    api_title: str = "CV Analyzer AI Service"
    api_version: str = "1.0.0"
    api_description: str = "Resume analysis using Azure AI Foundry and GPT-4o"
    
    # Azure Authentication
    azure_client_id: str | None = os.getenv("AZURE_CLIENT_ID")
    azure_tenant_id: str | None = os.getenv("AZURE_TENANT_ID")
    azure_client_secret: str | None = os.getenv("AZURE_CLIENT_SECRET")
    
    # Performance
    max_analysis_length: int = 10000  # Max characters for analysis
    request_timeout: int = 30  # Seconds
    
    class Config:
        env_file = ".env"
        case_sensitive = False


@lru_cache
def get_settings() -> Settings:
    """Get cached settings instance"""
    return Settings()
