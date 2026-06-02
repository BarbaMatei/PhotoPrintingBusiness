---
id: 003-locker-map-component
unit: 005-checkout-ui
intent: 004-checkout-payment
status: complete
priority: must
created: 2026-05-21T12:00:00Z
assigned_bolt: 017-checkout-ui
implemented: true
---

# Story: 003-locker-map-component

## User Story

**As a** customer choosing Easybox delivery
**I want** to search for lockers by city and click one on a map
**So that** I can choose a convenient pickup point near me

## Acceptance Criteria

- [ ] **Given** the locker map component is shown, **When** a city name is typed in the search box, **Then** after 300ms debounce, `GET /api/shipping/lockers?city={query}` is called and locker pins appear on the map
- [ ] **Given** lockers are returned, **When** rendered on the map, **Then** Leaflet with OpenStreetMap tile layer shows each locker as a clickable pin with a popup showing `name` and `address`
- [ ] **Given** a locker pin is clicked, **When** selected, **Then** the pin changes to a highlighted style, the locker address appears in a selection summary, and `lockerId` is stored in `CheckoutStateService`
- [ ] **Given** no city is entered, **When** the component first loads, **Then** the map is centered on Romania (lat 45.9, lng 24.9, zoom 7) with no pins
- [ ] **Given** the search returns no lockers, **When** the query resolves, **Then** the map shows no pins and a `"Nu am găsit locații pentru acest oraș"` message is shown

## Technical Notes

- Install `leaflet` + `@types/leaflet`; load Leaflet CSS in `angular.json styles`
- Use lazy-loaded Leaflet (dynamic `import('leaflet')`) to avoid SSR issues if SSR is ever added
- Map component: Angular standalone, uses `AfterViewInit` lifecycle hook to initialize Leaflet map on `div#map`
- Debounce: `fromEvent(searchInput, 'input').pipe(debounceTime(300), distinctUntilChanged(), switchMap(...))`
- Selected locker pin: custom blue DivIcon vs. default for unselected
- Map container height: `400px` on desktop, `250px` on mobile (CSS class)
- `ngx-leaflet` is optional — raw Leaflet is sufficient and avoids an extra dependency

## Dependencies

### Requires
- Story 001-checkout-stepper (CheckoutStateService.setLocker)
- Bolt 015 (shipping-and-order-core — `/api/shipping/lockers` endpoint)

### Enables
- Story 002-delivery-step (locker map is embedded in delivery step)

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Lockers API returns 500 | Error toast; map shows empty state |
| User selects a locker then changes city | Previous selection cleared; new pin must be re-selected |
| Map container not yet in DOM on init | Guard with `if (!document.getElementById('map')) return` |
| Very long city search term (>100 chars) | Client-side truncation before API call |

## Out of Scope

- Geolocation / "use my location" feature
- Distance sorting of locker results
- Locker photos / capacity info
