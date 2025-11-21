"""
Configuration management for the Phonetic Search application.
Loads settings from environment variables with sensible defaults.
"""
from typing import List
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Application settings loaded from environment variables."""

    # Database Configuration
    database_url: str = "postgresql://postgres:postgres@localhost:5432/phonetic_native"
    db_pool_min_size: int = 2
    db_pool_max_size: int = 10
    db_pool_max_queries: int = 50000
    db_pool_max_inactive_connection_lifetime: float = 300.0

    # Server Configuration
    host: str = "0.0.0.0"
    port: int = 8000
    reload: bool = True
    log_level: str = "info"

    # Application Settings
    app_name: str = "Phonetic Search UI"
    app_version: str = "1.0.0"
    debug: bool = True
    api_base_url: str = "http://localhost:5116"

    # CORS Settings
    allowed_origins: List[str] = [
        "http://localhost:3000",
        "http://localhost:8000",
    ]

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,
        extra="ignore",
    )


# Global settings instance
settings = Settings()
