---
intent: 005-order-management
phase: inception
status: complete
created: 2026-05-22T07:10:00Z
updated: 2026-05-22T07:20:00Z
---

# Requirements: Order Management

## Intent Overview

Expose a complete Order Management capability: a backend API that lets customers retrieve their order history and order details, and two Angular pages (Order History List and Order Detail) that present this data. Orders are created by the payment flow (bolts 015–017); this intent adds the read/query layer on top of the existing Order entity.

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Customers can see all their past orders | Order list loads in < 1 s with correct data | Must |
| Customers can review the details of a single order | Order detail shows items, totals, delivery, status | Must |
| Guest customers who later register can see their orders | Claimed orders appear in authenticated history | Should |
| Admin can query orders (foundation for Phase 6) | Orders API is usable by admin panel later | Should |

---

## Functional Requirements

### FR-1: Orders List Endpoint
- **Description**: `GET /api/orders` returns the authenticated user's orders, newest first. Supports pagination (`page`, `pageSize`). Guests return empty list.
- **Acceptance Criteria**: Returns `[{ id, orderNumber, status, totalRon, createdAt, deliveryType, itemCount }]`; max 50 per page; includes `X-Total-Count` header.
- **Priority**: Must
- **Related Stories**: US-403

### FR-2: Order Detail Endpoint
- **Description**: `GET /api/orders/{id}` returns the full order including line items, delivery info, payment processor, and status timeline.
- **Acceptance Criteria**: Returns full `OrderDto`; 404 for unknown id; 403 if order does not belong to requesting user.
- **Priority**: Must
- **Related Stories**: US-403

### FR-3: Order History Page (Frontend)
- **Description**: Angular page at `/comenzi` listing the authenticated user's orders with status badges, order numbers, totals, and dates. Requires login (redirects guests).
- **Acceptance Criteria**: Paginated list; status badge colour-coded; clicking an order navigates to detail page.
- **Priority**: Must
- **Related Stories**: US-401

### FR-4: Order Detail Page (Frontend)
- **Description**: Angular page at `/comenzi/:id` showing full order breakdown: product photos (thumbnails), quantity, per-item total, shipping address or locker, cost summary, payment method, status stepper.
- **Acceptance Criteria**: Matches design spec; 404 → redirect to `/comenzi`; status stepper matches `ConfirmationPage` stepper.
- **Priority**: Must
- **Related Stories**: US-402

### FR-5: Order Status Labels (Shared)
- **Description**: Consistent Romanian status labels and colour tokens for `Pending`, `Paid`, `Printing`, `Shipped`, `Delivered`, `Cancelled` used across history list, detail, and confirmation pages.
- **Acceptance Criteria**: Single source of truth constant/pipe used in all three pages.
- **Priority**: Must

---

## Non-Functional Requirements

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Orders list API | Response time | < 300 ms (p95) |
| Pagination | Default page size | 10, max 50 |

### Security
| Requirement | Detail |
|-------------|--------|
| Ownership check | `GET /api/orders/{id}` returns 403 if order.UserId ≠ current user |
| Auth required | `/comenzi` and `/comenzi/:id` routes guarded by `authGuard` |

### Accessibility
| Requirement | Detail |
|-------------|--------|
| Status badges | Colour + icon (not colour alone) to convey status |

---

## Out of Scope

- Cancelling an order (future admin/user story)
- Invoice PDF download
- Admin order queue (Phase 6 — US-504)
- Order search / filtering beyond pagination
