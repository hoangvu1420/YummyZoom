read and explore the docs and code in the project to understand the context and pattern, start with @Docs/Architecture/YummyZoom_Project_Documentation.md . 
 the frontend team request a few changes to the API, please implement them.

- `GET /api/v1/restaurants/{restaurantId}/info`
  - ✅ Name, logoUrl, cuisineTags, isAcceptingOrders, city, avgRating, ratingCount
  - ⛔ Distance (requires calling context to supply)
  - ⛔ Hero/cover image, phone/address, geo coords (except city)

you can reference other features in the codebase for the pattern of implementation. (we got restaurant/search that returns the distance).

analyze and give a step by step plan to implement this feature. write the plan in this file under this line. no code, just the plan.

---

## Implementation Plan: Add Optional Distance to Restaurant Info Endpoint

### Overview
Add optional distance calculation to `GET /api/v1/restaurants/{restaurantId}/info` endpoint. When caller provides `lat` and `lng` query parameters, the endpoint will calculate and return the distance in kilometers from the provided location to the restaurant. This follows the same pattern used in `/restaurants/search`.

### Current State Analysis
- **Already implemented**: name, logoUrl, cuisineTags, isAcceptingOrders, city, avgRating, ratingCount
- **Needs implementation**: Optional distance calculation when lat/lng provided
- **Reference pattern**: `SearchRestaurantsQuery` and `SearchRestaurantsQueryHandler` demonstrate the distance calculation pattern using Haversine formula in PostgreSQL

### Implementation Steps

#### 1. Application Layer - Query Object
**File**: `src/Application/Restaurants/Queries/GetRestaurantPublicInfo/GetRestaurantPublicInfoQuery.cs`

**Changes**:
- Add two optional parameters: `double? Lat` and `double? Lng` to the query record
- **Caching consideration**: The query implements `ICacheableQuery<Result<RestaurantPublicInfoDto>>`
  - **Decision needed**: Should we:
    - Option A: Disable caching entirely when lat/lng are provided (bypass cache for personalized queries)
    - Option B: Include lat/lng in cache key (creates many cache variations, not recommended)
    - Option C: Cache without distance, always calculate distance fresh when lat/lng provided
  - **Recommendation**: Option A - Check if lat/lng are provided; if yes, skip caching behavior OR adjust cache key to include "distance-requested" flag
- Current cache key pattern: `restaurant:public-info:v1:{restaurantId:N}`
- If we keep caching with distance: bump cache version to `v2` and include lat/lng in key (but this is inefficient)
- **Best approach**: Keep the query cacheable for basic info (without lat/lng), but the caching behavior should be smart enough to bypass cache when personalized data (lat/lng) is requested

#### 2. Application Layer - Query Validator
**File**: `src/Application/Restaurants/Queries/GetRestaurantPublicInfo/GetRestaurantPublicInfoQueryValidator.cs`

**Changes**:
- Add validation rules for optional `Lat` and `Lng` parameters:
  - When provided, latitude must be between -90 and 90
  - When provided, longitude must be between -180 and 180
  - Both should be provided together or both null (validate that if one is provided, the other must be too)
- Use FluentValidation conditional rules (`.When()`)

#### 3. Application Layer - Query Handler
**File**: `src/Application/Restaurants/Queries/GetRestaurantPublicInfo/GetRestaurantPublicInfoQueryHandler.cs`

**Changes**:
- Update SQL query to include distance calculation using the same Haversine formula from `SearchRestaurantsQueryHandler`:
  ```
  CASE 
      WHEN CAST(@Lat AS double precision) IS NOT NULL AND CAST(@Lng AS double precision) IS NOT NULL 
           AND r."Geo_Latitude" IS NOT NULL AND r."Geo_Longitude" IS NOT NULL THEN
          6371 * 2 * ASIN(SQRT(POWER(SIN(RADIANS((CAST(@Lat AS double precision) - r."Geo_Latitude")/2)),2) 
              + COS(RADIANS(CAST(@Lat AS double precision))) * COS(RADIANS(r."Geo_Latitude")) 
              * POWER(SIN(RADIANS((CAST(@Lng AS double precision) - r."Geo_Longitude")/2)),2)))
      ELSE NULL
  END AS DistanceKm
  ```
- Add `Lat` and `Lng` parameters to the SQL command definition
- Update `RestaurantPublicInfoRow` private record to include `decimal? DistanceKm` property
- Pass `DistanceKm` from row to the DTO constructor

#### 4. Application Layer - DTO
**File**: `src/Application/Restaurants/Queries/Common/RestaurantDtos.cs`

**Changes**:
- Add `decimal? DistanceKm` parameter to `RestaurantPublicInfoDto` record
- Position: Add as the last parameter to maintain backward compatibility in positional record syntax
- This DTO is shared between query handler and web endpoint

#### 5. Web Layer - Response DTO
**File**: `src/Web/Endpoints/Restaurants.cs`

**Changes**:
- Update `RestaurantPublicInfoResponseDto` record to include `decimal? DistanceKm` parameter
- Add as the last parameter for consistency
- This mirrors the application DTO but is specific to the Web API contract

#### 6. Web Layer - Endpoint
**File**: `src/Web/Endpoints/Restaurants.cs` (around line 707)

**Changes**:
- Add optional query parameters `double? lat` and `double? lng` to the endpoint method signature
- Pass `lat` and `lng` to the `GetRestaurantPublicInfoQuery` constructor
- Update the mapping from `RestaurantPublicInfoDto` to `RestaurantPublicInfoResponseDto` to include `DistanceKm`
- Update endpoint description to mention the optional distance calculation feature

#### 7. Test Layer - Contract Tests
**File**: `tests/Web.ApiContractTests/Restaurants/InfoContractTests.cs`

**Changes**:
- Add new test: `GetRestaurantInfo_WhenLatLngProvided_ReturnsDistanceKm`
  - Mock a restaurant with known coordinates
  - Call endpoint with lat/lng query parameters
  - Assert that `distanceKm` field is present and has a numeric value
- Add new test: `GetRestaurantInfo_WhenLatLngNotProvided_ReturnsNullDistance`
  - Call endpoint without lat/lng
  - Assert that `distanceKm` is null or absent
- Update existing tests to verify that `distanceKm` field is properly optional and doesn't break existing contracts

#### 8. Test Layer - Functional Tests
**File**: `tests/Application.FunctionalTests/Features/Restaurants/Queries/GetRestaurantPublicInfoQueryTests.cs`

**Changes**:
- Add test: `Handle_WhenLatLngProvidedAndRestaurantHasCoordinates_CalculatesDistance`
  - Create a restaurant with known Geo_Latitude and Geo_Longitude in database
  - Send query with lat/lng
  - Assert calculated distance is approximately correct (within acceptable margin)
- Add test: `Handle_WhenLatLngNotProvided_ReturnsNullDistance`
  - Send query without lat/lng
  - Assert DistanceKm is null
- Add test: `Handle_WhenRestaurantHasNoCoordinates_ReturnsNullDistance`
  - Create restaurant without coordinates
  - Send query with lat/lng
  - Assert DistanceKm is null (can't calculate distance without restaurant coordinates)

#### 9. Documentation - API Reference
**File**: `Docs/API-Documentation/API-Reference/Customer/02-Restaurant-Discovery.md`

**Changes** (around line 447-500):
- Add new section under "Query Parameters" for the info endpoint:
  | Parameter | Type | Required | Description |
  |-----------|------|----------|-------------|
  | `lat` | `number` | No | Latitude for distance calculation (-90 to 90) |
  | `lng` | `number` | No | Longitude for distance calculation (-180 to 180) |
- Add note: "Both `lat` and `lng` must be provided together to calculate distance. If only one is provided, an error will be returned."
- Update the response example to show both scenarios:
  - Example 1: Without lat/lng (distanceKm is null)
  - Example 2: With lat/lng (distanceKm has a value like 2.3)
- Update `Restaurant Info Object` table to include:
  | `distanceKm` | `number|null` | Distance in kilometers from provided location (only when lat/lng supplied) |
- Add validation error example for invalid coordinates

#### 10. Additional Considerations

**Database Requirements**:
- Verify that the `Restaurants` table has `Geo_Latitude` and `Geo_Longitude` columns (already present based on SearchRestaurants implementation)
- These columns should be nullable (restaurants might not have coordinates)

**Performance Considerations**:
- Distance calculation is done in SQL, which is efficient
- No additional database index needed (single-row lookup by ID)
- The CASE statement ensures calculation only happens when all required data is present

**Error Handling**:
- Validator will catch invalid coordinate ranges
- SQL will return NULL distance if restaurant has no coordinates
- Frontend should handle NULL distance gracefully (don't show distance badge)

**Backward Compatibility**:
- Adding optional query parameters doesn't break existing clients
- Adding optional response field doesn't break existing clients
- Cache version bump (if needed) won't affect runtime behavior

**Caching Strategy** (Final Decision Needed):
- **Recommended Approach**: Modify the caching behavior to bypass cache when lat/lng are provided
  - Check in the CachingBehavior or in the query implementation if personalization parameters exist
  - If lat/lng are null: use cache (basic info)
  - If lat/lng provided: bypass cache, execute query fresh (personalized distance)
- This keeps the benefits of caching for the common case while supporting personalized queries

### Testing Strategy Summary

1. **Unit Tests** (Validator): Lat/lng validation rules
2. **Functional Tests** (Query Handler): Distance calculation accuracy, NULL handling
3. **Contract Tests** (Web Endpoint): API shape, optional fields, error responses
4. **Manual Testing**: Test with real coordinates and verify distances using external tools

### Rollout Notes

- Feature is backward compatible
- No database migration required
- No breaking changes to API contract
- Frontend can adopt incrementally (start sending lat/lng when available)
- Existing clients continue to work without changes

### Reference Files for Pattern Matching

- **Distance Calculation Pattern**: `src/Application/Restaurants/Queries/SearchRestaurants/SearchRestaurantsQueryHandler.cs` (lines 26-40)
- **Optional Parameters Pattern**: `src/Application/Restaurants/Queries/SearchRestaurants/SearchRestaurantsQuery.cs` (lines 7-18)
- **DTO with Optional Distance**: `src/Application/Restaurants/Queries/Common/RestaurantDtos.cs` (RestaurantSearchResultDto)
- **Caching Implementation**: `src/Application/Restaurants/Queries/GetRestaurantPublicInfo/GetRestaurantPublicInfoQuery.cs` (ICacheableQuery)

---

### Implementation Order

1. Start with Application Layer (Query, Validator, Handler, DTO)
2. Then Web Layer (Endpoint, Response DTO)
3. Then Tests (Functional first, then Contract)
4. Finally Documentation

This ensures a clean dependency flow: Domain → Application → Web → Tests → Docs
