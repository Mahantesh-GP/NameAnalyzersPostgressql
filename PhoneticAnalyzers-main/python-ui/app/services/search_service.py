"""Search service using C# API backend."""
import httpx
from typing import List
import time
from app.models.search import PersonMatch, MatchType
from app.config import settings


class SearchService:
    """Handles search operations via C# API."""

    def __init__(self):
        self.api_base_url = getattr(settings, 'api_base_url', 'http://localhost:5116')
    
    async def search_persons(
        self,
        query_name: str,
        county_id: int | None = None,
        record_type: str | None = None,
        min_similarity_threshold: float = 50.0,
        max_results: int = 200,
        expand_nicknames: bool = True,
        include_trigram_similarity: bool = False,
        include_phonetic: bool = False,
    ) -> tuple[List[PersonMatch], float]:
        """
        Execute search via C# API.

        Returns:
            Tuple of (matches, execution_time_ms)
        """
        start_time = time.perf_counter()

        try:
            async with httpx.AsyncClient(timeout=30.0) as client:
                # Build request payload matching Blazor UI
                payload = {
                    "queryName": query_name,
                    "maxResults": max_results,
                    "minSimilarityThreshold": min_similarity_threshold / 100.0,  # Convert % to decimal
                    "expandNicknames": expand_nicknames,
                    "includeTrigramSimilarity": include_trigram_similarity,
                    "includePhonetic": include_phonetic,
                    "includeMatchDetails": True,
                }
                
                if county_id is not None:
                    payload["countyId"] = county_id
                if record_type is not None:
                    payload["recordType"] = record_type

                # Call C# API advanced search endpoint
                response = await client.post(
                    f"{self.api_base_url}/api/search/advanced",
                    json=payload,
                )
                response.raise_for_status()
                
                data = response.json()
                
                # Parse results
                matches = []
                for result in data.get("results", []):
                    matches.append(
                        PersonMatch(
                            person_id=result.get("personId", 0),
                            external_id=result.get("externalId"),
                            full_name=result.get("fullName", ""),
                            normalized_name=result.get("normalizedName"),
                            county=result.get("county", ""),
                            county_id=result.get("countyId", 0),
                            county_name=result.get("countyName", ""),
                            flag=result.get("flag", ""),
                            match_type=MatchType(result.get("matchType", "Exact")),
                            similarity_score=result.get("similarityScore", 0) * 100,  # Convert to percentage
                            match_metadata=result.get("matchMetadata"),
                        )
                    )

        except httpx.HTTPError as e:
            raise RuntimeError(f"API request failed: {e}") from e
        except Exception as e:
            raise RuntimeError(f"Search failed: {e}") from e

        execution_time_ms = (time.perf_counter() - start_time) * 1000
        return matches, execution_time_ms

    async def get_suggestions(
        self,
        prefix: str,
        limit: int = 10,
    ) -> List[str]:
        """
        Get name autocomplete suggestions.

        Args:
            prefix: Search prefix
            limit: Max suggestions to return
        """
        if not prefix or len(prefix) < 2:
            return []

        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                response = await client.get(
                    f"{self.api_base_url}/api/search/suggestions",
                    params={"prefix": prefix, "limit": limit},
                )
                response.raise_for_status()
                data = response.json()
                
                # C# API returns {"suggestions": [...]}
                if isinstance(data, dict) and "suggestions" in data:
                    return data["suggestions"]
                # Fallback if API returns list directly
                elif isinstance(data, list):
                    return data
                else:
                    return []

        except httpx.HTTPError as e:
            raise RuntimeError(f"API request failed: {e}") from e
        except Exception as e:
            raise RuntimeError(f"Suggestions query failed: {e}") from e
