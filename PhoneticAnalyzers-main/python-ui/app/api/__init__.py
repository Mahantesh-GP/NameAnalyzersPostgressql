"""API routes package."""
from .search import router as search_router
from .health import router as health_router

__all__ = ["search_router", "health_router"]
