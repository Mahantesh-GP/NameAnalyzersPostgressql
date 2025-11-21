"""Health check endpoint."""
from fastapi import APIRouter, HTTPException
import httpx
from app.config import settings

router = APIRouter(tags=["health"])


@router.get("/health")
async def health_check() -> dict:
    """
    Health check endpoint.

    Returns application and API backend connection status.
    """
    try:
        # Test C# API connection
        async with httpx.AsyncClient(timeout=5.0) as client:
            response = await client.get(f"{settings.api_base_url}/health")
            api_status = "healthy" if response.status_code == 200 else "degraded"

    except Exception as e:
        api_status = "unhealthy"
        raise HTTPException(
            status_code=503,
            detail=f"API backend connection failed: {e}",
        ) from e

    return {
        "status": "healthy",
        "api_backend": api_status,
        "api_url": settings.api_base_url,
        "version": settings.app_version,
    }
