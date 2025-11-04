"""
FastAPI application for CV Analyzer AI Service
"""
import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI, HTTPException, status
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse

from app.config import get_settings
from app.models import (
    ResumeAnalysisRequest,
    ResumeAnalysisResponse,
    HealthCheckResponse
)
from app.agent import get_agent, cleanup_agent

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

settings = get_settings()


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Manage application lifecycle"""
    # Startup
    logger.info("Starting CV Analyzer AI Service...")
    try:
        # Initialize agent
        await get_agent()
        logger.info("Agent initialized successfully")
    except Exception as e:
        logger.error(f"Failed to initialize agent: {e}")
        raise
    
    yield
    
    # Shutdown
    logger.info("Shutting down CV Analyzer AI Service...")
    await cleanup_agent()
    logger.info("Shutdown complete")


# Create FastAPI application
app = FastAPI(
    title=settings.api_title,
    version=settings.api_version,
    description=settings.api_description,
    lifespan=lifespan
)

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Configure based on environment
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/", tags=["Root"])
async def root():
    """Root endpoint"""
    return {
        "service": settings.service_name,
        "version": settings.api_version,
        "status": "running"
    }


@app.get("/health", response_model=HealthCheckResponse, tags=["Health"])
async def health_check():
    """Health check endpoint"""
    try:
        agent = await get_agent()
        ai_connected = agent._agent is not None
    except Exception:
        ai_connected = False
    
    return HealthCheckResponse(
        status="healthy" if ai_connected else "degraded",
        version=settings.api_version,
        ai_foundry_connected=ai_connected
    )


@app.post(
    "/analyze",
    response_model=ResumeAnalysisResponse,
    status_code=status.HTTP_200_OK,
    tags=["Analysis"]
)
async def analyze_resume(request: ResumeAnalysisRequest):
    """
    Analyze a resume and provide optimization suggestions
    
    Args:
        request: Resume analysis request with content and user_id
        
    Returns:
        Analysis results with score, optimized content, and suggestions
    """
    try:
        logger.info(f"Received analysis request for user: {request.user_id}")
        
        # Get agent instance
        agent = await get_agent()
        
        # Perform analysis
        score, optimized_content, suggestions, metadata = await agent.analyze_resume(
            content=request.content,
            user_id=request.user_id
        )
        
        # Build response
        response = ResumeAnalysisResponse(
            score=score,
            optimized_content=optimized_content,
            suggestions=suggestions,
            analysis_metadata=metadata
        )
        
        logger.info(
            f"Analysis completed for user: {request.user_id}, "
            f"score: {score}, suggestions: {len(suggestions)}"
        )
        
        return response
        
    except ValueError as e:
        # Client errors (validation, timeout, etc.)
        logger.warning(f"Client error during analysis: {e}")
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(e)
        )
    except Exception as e:
        # Server errors
        logger.error(f"Server error during analysis: {e}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="An error occurred during resume analysis. Please try again later."
        )


@app.exception_handler(Exception)
async def global_exception_handler(request, exc):
    """Global exception handler"""
    logger.error(f"Unhandled exception: {exc}", exc_info=True)
    return JSONResponse(
        status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
        content={
            "detail": "An unexpected error occurred. Please try again later."
        }
    )


if __name__ == "__main__":
    import uvicorn
    
    uvicorn.run(
        "app.main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
        log_level=settings.log_level.lower()
    )
