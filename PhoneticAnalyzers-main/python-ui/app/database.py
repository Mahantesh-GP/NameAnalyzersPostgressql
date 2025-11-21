"""
Database connection management using asyncpg connection pool.
Provides lifecycle management for database connections.
"""
import asyncpg
from typing import Optional
from app.config import settings
import logging

logger = logging.getLogger(__name__)


class DatabaseManager:
    """Manages asyncpg connection pool lifecycle."""

    def __init__(self):
        self.pool: Optional[asyncpg.Pool] = None

    async def connect(self) -> None:
        """Create database connection pool."""
        if self.pool is not None:
            logger.warning("Database pool already initialized")
            return

        try:
            self.pool = await asyncpg.create_pool(
                dsn=settings.database_url,
                min_size=settings.db_pool_min_size,
                max_size=settings.db_pool_max_size,
                max_queries=settings.db_pool_max_queries,
                max_inactive_connection_lifetime=settings.db_pool_max_inactive_connection_lifetime,
                command_timeout=60,
            )
            logger.info(
                f"Database pool created (min={settings.db_pool_min_size}, "
                f"max={settings.db_pool_max_size})"
            )
        except Exception as e:
            logger.error(f"Failed to create database pool: {e}")
            raise

    async def disconnect(self) -> None:
        """Close database connection pool."""
        if self.pool is None:
            logger.warning("Database pool not initialized")
            return

        try:
            await self.pool.close()
            logger.info("Database pool closed")
            self.pool = None
        except Exception as e:
            logger.error(f"Error closing database pool: {e}")
            raise

    def get_pool(self) -> asyncpg.Pool:
        """Get the connection pool instance."""
        if self.pool is None:
            raise RuntimeError("Database pool not initialized. Call connect() first.")
        return self.pool


# Global database manager instance
db_manager = DatabaseManager()


async def get_db_pool() -> asyncpg.Pool:
    """Dependency injection for FastAPI routes."""
    return db_manager.get_pool()
