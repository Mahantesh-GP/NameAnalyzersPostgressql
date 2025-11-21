"""Search API endpoints."""
from fastapi import APIRouter, HTTPException, Query, Request
from fastapi.responses import HTMLResponse
from fastapi.templating import Jinja2Templates
from typing import List
from app.models.search import SearchRequest, SearchResponse, MatchType
from app.services.search_service import SearchService
from app.services.county_service import CountyService, CountyInfo

router = APIRouter(prefix="/api", tags=["search"])
templates = Jinja2Templates(directory="templates")


def filter_and_distribute_results(
    all_matches: list,
    max_results: int,
    expand_nicknames: bool,
    include_fuzzy: bool,
    include_phonetic: bool,
) -> list:
    """
    Filter matches by strategy and distribute results proportionally.

    Always includes Exact matches. Distributes remaining slots among
    enabled strategies (Nickname/Fuzzy/Phonetic).
    """
    # Separate by match type
    exact = [m for m in all_matches if m.match_type == MatchType.EXACT]
    nickname = [m for m in all_matches if m.match_type == MatchType.NICKNAME]
    fuzzy = [m for m in all_matches if m.match_type == MatchType.TRIGRAM]
    phonetic = [m for m in all_matches if m.match_type == MatchType.PHONETIC]

    # Start with exact matches (always included)
    filtered = exact[:max_results]
    remaining_slots = max(0, max_results - len(filtered))

    if remaining_slots == 0:
        return filtered

    # Count enabled strategies (excluding Exact)
    enabled_strategies = []
    if expand_nicknames:
        enabled_strategies.append(("nickname", nickname))
    if include_fuzzy:
        enabled_strategies.append(("fuzzy", fuzzy))
    if include_phonetic:
        enabled_strategies.append(("phonetic", phonetic))

    if not enabled_strategies:
        return filtered

    # Calculate slots per strategy
    slots_per_strategy = remaining_slots // len(enabled_strategies)
    extra_slots = remaining_slots % len(enabled_strategies)

    # Distribute results
    for idx, (strategy_name, matches) in enumerate(enabled_strategies):
        slots = slots_per_strategy + (1 if idx < extra_slots else 0)
        filtered.extend(matches[:slots])

    return filtered


@router.post("/search", response_class=HTMLResponse)
async def search(
    http_request: Request,
    request: SearchRequest,
):
    """
    Search for persons using phonetic matching strategies via C# API.

    - **query_name**: Name to search (required)
    - **county_id**: County ID filter (optional)
    - **record_type**: Record type (I=Individual, B=Business, U=Unknown)
    - **min_similarity_threshold**: Minimum similarity percentage (0-100)
    - **max_results**: Maximum results to return (1-200)
    - **expand_nicknames**: Include nickname matches (default: true)
    - **include_trigram_similarity**: Include fuzzy matches (default: false)
    - **include_phonetic**: Include phonetic matches (default: false)
    - **view_type**: Result view type ('list' or 'grouped')
    """
    if not request.has_search_criteria():
        raise HTTPException(
            status_code=400,
            detail="Query name is required",
        )

    try:
        service = SearchService()

        # Fetch results from C# API (up to 200)
        all_matches, search_time_ms = await service.search_persons(
            query_name=request.query_name,
            county_id=request.county_id,
            record_type=request.record_type,
            min_similarity_threshold=request.min_similarity_threshold,
            max_results=200,  # Fetch extra for client filtering
            expand_nicknames=request.expand_nicknames,
            include_trigram_similarity=request.include_trigram_similarity,
            include_phonetic=request.include_phonetic,
        )

        # Filter and distribute by strategy
        filtered_matches = filter_and_distribute_results(
            all_matches=all_matches,
            max_results=request.max_results,
            expand_nicknames=request.expand_nicknames,
            include_fuzzy=request.include_trigram_similarity,
            include_phonetic=request.include_phonetic,
        )

        # Prepare response data for template
        response_data = SearchResponse(
            matches=filtered_matches,
            total_count=len(filtered_matches),
            search_time_ms=search_time_ms,
            filters_applied={
                "expand_nicknames": request.expand_nicknames,
                "include_fuzzy": request.include_trigram_similarity,
                "include_phonetic": request.include_phonetic,
                "min_similarity": request.min_similarity_threshold,
            },
        )

        # Render HTML template for HTMX
        return templates.TemplateResponse(
            "components/results.html",
            {
                "request": http_request,
                "matches": response_data.matches,
                "total_count": response_data.total_count,
                "search_time_ms": response_data.search_time_ms,
                "filters_applied": response_data.filters_applied,
                "view_type": request.view_type.value,
                "exact_matches": response_data.exact_matches,
                "nickname_matches": response_data.nickname_matches,
                "fuzzy_matches": response_data.fuzzy_matches,
                "phonetic_matches": response_data.phonetic_matches,
            },
        )

    except RuntimeError as e:
        raise HTTPException(status_code=500, detail=str(e)) from e


@router.get("/suggestions", response_model=List[str])
async def get_suggestions(
    prefix: str = Query(..., min_length=2, description="Search prefix"),
    limit: int = Query(10, ge=1, le=50, description="Max suggestions"),
) -> List[str]:
    """
    Get autocomplete suggestions for names.

    - **prefix**: Search prefix (min 2 characters)
    - **limit**: Maximum suggestions to return (1-50)
    """
    try:
        service = SearchService()
        suggestions = await service.get_suggestions(
            prefix=prefix,
            limit=limit,
        )
        return suggestions

    except RuntimeError as e:
        raise HTTPException(status_code=500, detail=str(e)) from e


@router.get("/counties", response_model=List[CountyInfo])
async def get_counties() -> List[CountyInfo]:
    """Get list of all counties with ID, code, and name."""
    try:
        service = CountyService()
        counties = await service.get_all_counties()
        return counties

    except RuntimeError as e:
        raise HTTPException(status_code=500, detail=str(e)) from e
