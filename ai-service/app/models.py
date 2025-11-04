"""
Pydantic models for request/response validation
"""
from pydantic import BaseModel, Field, field_validator
from typing import List


class ResumeAnalysisRequest(BaseModel):
    """Request model for resume analysis"""
    
    content: str = Field(
        ..., 
        description="Resume content to analyze",
        min_length=10,
        max_length=10000
    )
    user_id: str = Field(
        ..., 
        description="User ID for tracking",
        min_length=1,
        max_length=100
    )
    
    @field_validator('content')
    @classmethod
    def validate_content(cls, v: str) -> str:
        """Validate content is not empty or just whitespace"""
        if not v.strip():
            raise ValueError("Content cannot be empty or whitespace")
        return v.strip()
    
    model_config = {
        "json_schema_extra": {
            "examples": [
                {
                    "content": "Software Engineer with 5 years experience in Python...",
                    "user_id": "user123"
                }
            ]
        }
    }


class Suggestion(BaseModel):
    """Individual improvement suggestion"""
    
    category: str = Field(..., description="Suggestion category")
    description: str = Field(..., description="Detailed suggestion")
    priority: int = Field(..., ge=1, le=5, description="Priority (1=highest, 5=lowest)")
    
    model_config = {
        "json_schema_extra": {
            "examples": [
                {
                    "category": "Skills",
                    "description": "Add more specific technical skills",
                    "priority": 1
                }
            ]
        }
    }


class ResumeAnalysisResponse(BaseModel):
    """Response model for resume analysis"""
    
    score: float = Field(
        ..., 
        ge=0.0, 
        le=100.0,
        description="Overall resume score (0-100)"
    )
    optimized_content: str = Field(
        ..., 
        description="AI-optimized version of the resume"
    )
    suggestions: List[Suggestion] = Field(
        default_factory=list,
        description="List of improvement suggestions"
    )
    analysis_metadata: dict = Field(
        default_factory=dict,
        description="Additional analysis metadata"
    )
    
    model_config = {
        "json_schema_extra": {
            "examples": [
                {
                    "score": 85.5,
                    "optimized_content": "Senior Software Engineer...",
                    "suggestions": [
                        {
                            "category": "Skills",
                            "description": "Add cloud platform experience",
                            "priority": 1
                        }
                    ],
                    "analysis_metadata": {
                        "processing_time_ms": 1234,
                        "model_used": "gpt-4o"
                    }
                }
            ]
        }
    }


class HealthCheckResponse(BaseModel):
    """Health check response"""
    
    status: str = Field(..., description="Service status")
    version: str = Field(..., description="API version")
    ai_foundry_connected: bool = Field(..., description="AI Foundry connection status")
