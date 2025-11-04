"""
Resume Analyzer Agent using Microsoft Agent Framework
"""
import asyncio
import json
import logging
from typing import Tuple, List, Dict
from datetime import datetime

from agent_framework import ChatAgent
from agent_framework_azure_ai import AzureAIAgentClient
from azure.identity.aio import DefaultAzureCredential

from app.config import get_settings
from app.models import Suggestion

logger = logging.getLogger(__name__)


class ResumeAnalyzerAgent:
    """Resume analyzer using Agent Framework with GPT-4o"""
    
    def __init__(self):
        self.settings = get_settings()
        self._agent: ChatAgent | None = None
    
    async def initialize(self):
        """Initialize the agent (call once at startup)"""
        try:
            logger.info("Initializing Resume Analyzer Agent...")
            
            # Create Azure AI Agent Client
            client = AzureAIAgentClient(
                project_endpoint=self.settings.ai_foundry_endpoint,
                model_deployment_name=self.settings.model_deployment_name,
                async_credential=DefaultAzureCredential(),
                agent_name="ResumeAnalyzer",
            )
            
            # Create Chat Agent with instructions
            self._agent = ChatAgent(
                chat_client=client,
                instructions=self._get_agent_instructions(),
            )
            
            logger.info("Resume Analyzer Agent initialized successfully")
            
        except Exception as e:
            logger.error(f"Failed to initialize agent: {e}")
            raise
    
    async def close(self):
        """Clean up agent resources"""
        if self._agent:
            await self._agent.__aexit__(None, None, None)
            self._agent = None
            logger.info("Agent closed")
    
    def _get_agent_instructions(self) -> str:
        """Get the system instructions for the agent"""
        return """You are an expert resume analyzer and career consultant with deep knowledge of:
- ATS (Applicant Tracking Systems) optimization
- Resume best practices across industries
- Skills assessment and gap analysis
- Professional writing and formatting

Your task is to analyze resumes and provide:
1. An overall quality score (0-100) based on:
   - Content quality and relevance
   - Formatting and structure
   - ATS compatibility
   - Skills presentation
   - Achievements and impact statements

2. An optimized version that:
   - Improves clarity and impact
   - Enhances ATS compatibility
   - Strengthens achievement statements
   - Maintains the candidate's authentic voice

3. Specific, actionable improvement suggestions categorized by:
   - Skills (technical and soft skills)
   - Experience (achievement statements, quantification)
   - Format (structure, layout, ATS optimization)
   - Content (grammar, clarity, relevance)
   - Impact (making accomplishments stand out)

IMPORTANT: Return your analysis as valid JSON with this exact structure:
{
  "score": <number 0-100>,
  "optimized_content": "<improved resume text>",
  "suggestions": [
    {
      "category": "<Skills|Experience|Format|Content|Impact>",
      "description": "<specific actionable suggestion>",
      "priority": <1-5, where 1 is highest>
    }
  ],
  "reasoning": "<brief explanation of the score>"
}

Be constructive, specific, and actionable in your feedback."""
    
    async def analyze_resume(
        self, 
        content: str, 
        user_id: str
    ) -> Tuple[float, str, List[Suggestion], Dict]:
        """
        Analyze a resume and return structured results
        
        Args:
            content: Resume text content
            user_id: User identifier for logging
            
        Returns:
            Tuple of (score, optimized_content, suggestions, metadata)
        """
        start_time = datetime.utcnow()
        
        try:
            if not self._agent:
                raise RuntimeError("Agent not initialized. Call initialize() first.")
            
            logger.info(f"Analyzing resume for user {user_id}, length: {len(content)} chars")
            
            # Truncate content if too long
            if len(content) > self.settings.max_analysis_length:
                content = content[:self.settings.max_analysis_length]
                logger.warning(f"Content truncated to {self.settings.max_analysis_length} chars")
            
            # Prepare the analysis prompt
            prompt = f"""Analyze this resume and provide a comprehensive evaluation:

RESUME CONTENT:
---
{content}
---

Provide your analysis in the specified JSON format."""
            
            # Get agent response
            async with self._agent as agent:
                thread = agent.get_new_thread()
                result = await asyncio.wait_for(
                    agent.run(prompt, thread=thread),
                    timeout=self.settings.request_timeout
                )
            
            # Parse the JSON response
            analysis = self._parse_agent_response(result.text)
            
            # Calculate processing time
            processing_time = (datetime.utcnow() - start_time).total_seconds() * 1000
            
            # Create metadata
            metadata = {
                "processing_time_ms": round(processing_time, 2),
                "model_used": self.settings.model_deployment_name,
                "content_length": len(content),
                "user_id": user_id,
                "timestamp": datetime.utcnow().isoformat()
            }
            
            logger.info(
                f"Analysis complete for user {user_id}. "
                f"Score: {analysis['score']}, Time: {processing_time:.2f}ms"
            )
            
            return (
                analysis["score"],
                analysis["optimized_content"],
                [Suggestion(**s) for s in analysis["suggestions"]],
                metadata
            )
            
        except asyncio.TimeoutError:
            logger.error(f"Analysis timeout for user {user_id}")
            raise ValueError("Analysis request timed out. Please try again.")
        except Exception as e:
            logger.error(f"Analysis failed for user {user_id}: {e}")
            raise
    
    def _parse_agent_response(self, response_text: str) -> Dict:
        """
        Parse the agent's JSON response
        
        Args:
            response_text: Raw response from agent
            
        Returns:
            Parsed analysis dictionary
        """
        try:
            # Try to extract JSON from markdown code blocks if present
            if "```json" in response_text:
                json_start = response_text.find("```json") + 7
                json_end = response_text.find("```", json_start)
                json_text = response_text[json_start:json_end].strip()
            elif "```" in response_text:
                json_start = response_text.find("```") + 3
                json_end = response_text.find("```", json_start)
                json_text = response_text[json_start:json_end].strip()
            else:
                json_text = response_text.strip()
            
            analysis = json.loads(json_text)
            
            # Validate required fields
            required_fields = ["score", "optimized_content", "suggestions"]
            for field in required_fields:
                if field not in analysis:
                    raise ValueError(f"Missing required field: {field}")
            
            # Validate score range
            if not 0 <= analysis["score"] <= 100:
                logger.warning(f"Score out of range: {analysis['score']}, clamping to 0-100")
                analysis["score"] = max(0, min(100, analysis["score"]))
            
            return analysis
            
        except json.JSONDecodeError as e:
            logger.error(f"Failed to parse agent response as JSON: {e}")
            logger.debug(f"Raw response: {response_text}")
            
            # Fallback: return default analysis
            return {
                "score": 70.0,
                "optimized_content": "Analysis parsing failed. Original content returned.",
                "suggestions": [
                    {
                        "category": "System",
                        "description": "Unable to parse AI response. Please try again.",
                        "priority": 1
                    }
                ],
                "reasoning": "Response parsing error"
            }


# Singleton instance
_agent_instance: ResumeAnalyzerAgent | None = None


async def get_agent() -> ResumeAnalyzerAgent:
    """Get or create the singleton agent instance"""
    global _agent_instance
    
    if _agent_instance is None:
        _agent_instance = ResumeAnalyzerAgent()
        await _agent_instance.initialize()
    
    return _agent_instance


async def cleanup_agent():
    """Cleanup the agent instance"""
    global _agent_instance
    
    if _agent_instance:
        await _agent_instance.close()
        _agent_instance = None
