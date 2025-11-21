"""County service for retrieving county lists."""
import httpx
from typing import List
from pydantic import BaseModel
from app.config import settings


class CountyInfo(BaseModel):
    """County information."""
    county_id: int
    county: str
    county_name: str


class CountyService:
    """Handles county-related operations via C# API."""

    def __init__(self):
        self.api_base_url = getattr(settings, 'api_base_url', 'http://localhost:5116')

    async def get_all_counties(self) -> List[CountyInfo]:
        """
        Retrieve list of all counties from API.

        Returns:
            List of CountyInfo objects.
        """
        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                response = await client.get(
                    f"{self.api_base_url}/api/counties"
                )
                response.raise_for_status()
                data = response.json()
                
                return [CountyInfo(**county) for county in data]

        except httpx.HTTPError as e:
            raise RuntimeError(f"API request failed: {e}") from e
        except Exception as e:
            raise RuntimeError(f"Failed to fetch counties: {e}") from e
