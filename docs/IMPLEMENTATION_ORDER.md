# FotoTipar — Implementation Order

This document defines the logical order in which stories should be implemented. Stories within the same phase can be worked on in parallel where dependencies allow.

---

## Phase 0 — Foundation (Must be done first)
These stories establish the infrastructure that ALL other stories depend on.

| Order | Story | Title | Type | Rationale |
|-------|-------|-------|------|-----------|
| 0.1 | **US-801** | Global Error Handling & Logging | BE | Every backend endpoint needs consistent error handling and logging |
| 0.2 | **US-802** | Security Baselines | BE | CORS, HTTPS, rate limiting, headers — needed before any endpoint goes live |
| 0.3 | **US-804** | Angular App Shell & Routing | FE | App structure, guards, interceptors — every FE component depends on this |
| 0.4 | **US-605** | Email Infrastructure | BE | IEmailService needed by registration, order confirmation, shipping, etc. |

---

## Phase 1 — Authentication & Accounts
The core user system that unlocks all user-facing features.

| Order | Story | Title | Type | Dependencies |
|-------|-------|-------|------|-------------|
| 1.1 | **US-102** | Register — Backend | BE | US-801 |
| 1.2 | **US-103** | Email Verification | BE+FE | US-102, US-605 |
| 1.3 | **US-105** | Login — JWT + Refresh Token | BE | US-102 |
| 1.4 | **US-101** | Register — Frontend | FE | US-102, US-804 |
| 1.5 | **US-104** | Login — Frontend | FE | US-105, US-804 |
| 1.6 | **US-107** | Google OAuth — Backend | BE | US-105 |
| 1.7 | **US-106** | Google OAuth — Frontend | FE | US-107, US-804 |
| 1.8 | **US-110** | Password Reset | BE+FE | US-102, US-105, US-605 |
| 1.9 | **US-109** | Guest Checkout — Backend | BE | US-801 |
| 1.10 | **US-108** | Guest Checkout — Frontend | FE | US-109, US-804 |

---

## Phase 2 — Product & Upload Core
The core business logic: products, photo upload, and cart.

| Order | Story | Title | Type | Dependencies |
|-------|-------|-------|------|-------------|
| 2.1 | **US-204** | Product Catalogue API | BE | US-801 |
| 2.2 | **US-202** | Photo Upload — Backend | BE | US-105, US-109, US-801 |
| 2.3 | **US-206** | Cart API | BE | US-202, US-204 |
| 2.4 | **US-201** | Bulk Photo Upload — Frontend | FE | US-202, US-804 |
| 2.5 | **US-203** | Format & Finish Selector — Frontend | FE | US-201, US-204 |
| 2.6 | **US-205** | Cart Page — Frontend | FE | US-206, US-203 |

---

## Phase 3 — Checkout & Payment
The complete checkout flow: delivery, review, payment, confirmation.

| Order | Story | Title | Type | Dependencies |
|-------|-------|-------|------|-------------|
| 3.1 | **US-302** | Shipping API | BE | US-801 |
| 3.2 | **US-305** | Stripe Payment — Backend | BE | US-206, US-801 |
| 3.3 | **US-306** | EuPlatesc Payment — Backend | BE | US-305 (shared Order model) |
| 3.4 | **US-301** | Delivery Method — Frontend | FE | US-302, US-804 |
| 3.5 | **US-303** | Order Review — Frontend | FE | US-301, US-205 |
| 3.6 | **US-304** | Payment — Frontend | FE | US-305, US-306, US-303 |
| 3.7 | **US-307** | Order Confirmation — Frontend | FE | US-304, US-403 |

---

## Phase 4 — Order Management
Order history for customers and the orders API.

| Order | Story | Title | Type | Dependencies |
|-------|-------|-------|------|-------------|
| 4.1 | **US-403** | Orders API | BE | US-305, US-306 |
| 4.2 | **US-401** | Order History List — Frontend | FE | US-403 |
| 4.3 | **US-402** | Order Detail Page — Frontend | FE | US-403 |

---

## Phase 5 — Email Notifications
Transactional emails triggered by order lifecycle events.

| Order | Story | Title | Type | Dependencies |
|-------|-------|-------|------|-------------|
| 5.1 | **US-601** | Welcome Email | BE | US-605, US-103 |
| 5.2 | **US-602** | Order Confirmed Email | BE | US-605, US-305/306 |
| 5.3 | **US-603** | Order Shipped Email | BE | US-605, US-504 |
| 5.4 | **US-604** | Order Delivered Email | BE | US-605, US-504 |

---

## Phase 6 — Admin Panel
The operator-facing administration interface.

| Order | Story | Title | Type | Dependencies |
|-------|-------|-------|------|-------------|
| 6.1 | **US-505** | Admin Stats API | BE | US-305/306, US-802 |
| 6.2 | **US-504** | Admin API (Orders, Workflow, SignalR) | BE | US-403, US-802 |
| 6.3 | **US-501** | Admin Dashboard — Frontend | FE | US-505, US-804 |
| 6.4 | **US-502** | Admin Order Queue — Frontend | FE | US-504 |
| 6.5 | **US-503** | Admin Order Detail & Workflow — Frontend | FE | US-504 |
| 6.6 | **US-506** | Admin Product Management — Frontend | FE | US-504 |

---

## Phase 7 — User Account & Legal
Profile management, saved addresses, and legal compliance pages.

| Order | Story | Title | Type | Dependencies |
|-------|-------|-------|------|-------------|
| 7.1 | **US-702** | Account API | BE | US-105, US-803 |
| 7.2 | **US-701** | User Profile Page — Frontend | FE | US-702 |
| 7.3 | **US-703** | Saved Addresses — Frontend | FE | US-702 |
| 7.4 | **US-704** | Legal Pages & Cookie Consent — Frontend | FE | US-804 |

---

## Phase 8 — Background Jobs & Polish
Cleanup jobs and final integration.

| Order | Story | Title | Type | Dependencies |
|-------|-------|-------|------|-------------|
| 8.1 | **US-803** | Background Jobs | BE | US-202, US-109, US-702 |

---

## Summary — Critical Path

```
US-801 → US-802 → US-102 → US-105 → US-204 → US-202 → US-206 → US-305 → US-403 → US-504
  ↓                  ↓         ↓                                      ↓
US-804 ──────→ US-101/104 → US-201 → US-203 → US-205 → US-301 → US-304 → US-307
                                                                    ↓
                                                              US-401/402
```

## Notes

- **BE and FE stories in the same phase can be developed in parallel** by different developers
- **Phase 0 is non-negotiable** — everything else depends on it
- **Phase 7 (Legal pages US-704)** can be done at any time after Phase 0 since it's static content
- **Email notifications (Phase 5)** can be deferred but should be ready before going to production
- **Admin panel (Phase 6)** can be developed in parallel with Phases 4-5 once the Orders API exists
- **Background jobs (Phase 8)** can be added incrementally — not blocking for MVP launch but needed for production hygiene
