"""Search-related data models."""
from typing import List, Optional
from pydantic import BaseModel, Field, field_validator
from enum import Enum
from datetime import date


class MatchType(str, Enum):
    """Match strategy types."""

    EXACT = "Exact"
    NICKNAME = "NicknameExpansion"
    TRIGRAM = "TrigramSimilarity"
    PHONETIC = "Phonetic"


class ViewType(str, Enum):
    """Result view types."""

    LIST = "list"
    GROUPED = "grouped"


class SearchRequest(BaseModel):
    """Search request parameters matching Blazor UI."""

    query_name: str = Field(..., min_length=1, max_length=200)
    county_id: Optional[int] = Field(None)
    record_type: Optional[str] = Field(None, pattern="^[IBU]$")
    min_similarity_threshold: float = Field(default=50.0, ge=0.0, le=100.0)
    max_results: int = Field(default=50, ge=1, le=200)
    expand_nicknames: bool = Field(default=True)
    include_trigram_similarity: bool = Field(default=False)
    include_phonetic: bool = Field(default=False)
    view_type: ViewType = Field(default=ViewType.LIST)

    @field_validator("query_name")
    @classmethod
    def strip_whitespace(cls, v: str) -> str:
        """Remove leading/trailing whitespace."""
        return v.strip()

    @field_validator("county_id", mode="before")
    @classmethod
    def empty_string_to_none_int(cls, v):
        """Convert empty string to None for county_id."""
        if v == "" or v is None:
            return None
        return v

    @field_validator("record_type", mode="before")
    @classmethod
    def empty_string_to_none_str(cls, v):
        """Convert empty string to None for record_type."""
        if v == "" or v is None:
            return None
        return v

    @field_validator("expand_nicknames", "include_trigram_similarity", "include_phonetic", mode="before")
    @classmethod
    def checkbox_to_bool(cls, v):
        """Convert checkbox values to boolean."""
        if v == "on" or v is True or v == "true":
            return True
        if v == "" or v is None or v is False or v == "false":
            return False
        return bool(v)

    @field_validator("max_results", "min_similarity_threshold", mode="before")
    @classmethod
    def string_to_number(cls, v):
        """Convert string numbers to actual numbers."""
        if v == "" or v is None:
            return None  # Will use default
        if isinstance(v, str):
            try:
                return int(v) if "." not in v else float(v)
            except ValueError:
                return None
        return v

    def has_search_criteria(self) -> bool:
        """Check if search criteria provided."""
        return bool(self.query_name and self.query_name.strip())


class PersonMatch(BaseModel):
    """Individual person match result."""

    person_id: int
    external_id: Optional[str] = None
    full_name: str
    normalized_name: Optional[str] = None
    county: str
    county_id: int
    county_name: str
    flag: str
    match_type: MatchType
    similarity_score: float = Field(ge=0.0, le=100.0)
    match_metadata: Optional[dict] = None

    class Config:
        """Pydantic configuration."""

        from_attributes = True


class SearchResponse(BaseModel):
    """Search response with matches."""

    matches: List[PersonMatch] = Field(default_factory=list)
    total_count: int = Field(ge=0)
    search_time_ms: float = Field(ge=0.0)
    filters_applied: dict = Field(default_factory=dict)

    @property
    def exact_matches(self) -> List[PersonMatch]:
        """Get all exact matches."""
        return [m for m in self.matches if m.match_type == MatchType.EXACT]

    @property
    def nickname_matches(self) -> List[PersonMatch]:
        """Get all nickname matches."""
        return [m for m in self.matches if m.match_type == MatchType.NICKNAME]

    @property
    def fuzzy_matches(self) -> List[PersonMatch]:
        """Get all fuzzy matches."""
        return [m for m in self.matches if m.match_type == MatchType.TRIGRAM]

    @property
    def phonetic_matches(self) -> List[PersonMatch]:
        """Get all phonetic matches."""
        return [m for m in self.matches if m.match_type == MatchType.PHONETIC]
