---
name: project-overview
description: FotoTipar photo printing website project overview. Use this skill when you need to understand the project architecture, business rules, tech stack, data model, or overall structure of the FotoTipar application.
---

## Application Summary

FotoTipar is a Romanian photo printing e-commerce website where customers upload photos, select print format/finish, and order prints delivered via Easybox lockers or home courier.

## Architecture

- **Frontend**: Angular 17+ SPA (standalone components, lazy-loaded modules)
- **Backend**: ASP.NET Core 8 Web API
- **Database**: PostgreSQL 16 (EF Core Code-First)
- **Real-time**: SignalR (admin order notifications)
- **Payments**: Stripe (international cards) + EuPlatesc (Romanian cards)
- **Shipping**: Sameday/Easybox (Phase 1: static locker list; Phase 2: API integration)
- **Email**: MailKit (dev) / SendGrid (prod) via `IEmailService` abstraction

## Key Business Rules

1. **Three auth modes**: Email+Password, Google OAuth, Guest Checkout
2. **Photo uploads**: max 30 files, max 50MB each; JPEG, PNG, HEIC accepted
3. **Products**: 3 formats (10×15, 13×18, 15×21) × 2 finishes (Lucios, Mat) = 6 products
4. **One format+finish per order** — applies to all photos in the batch
5. **Guest orders**: linked by guest token; can be claimed after registration
6. **Prices in RON** (Romanian Lei)
7. **All UI text in Romanian**
8. **Dual payment**: Stripe (embedded card form) or EuPlatesc (redirect to hosted page)
9. **Order lifecycle**: AwaitingPayment → Paid → Printing → Shipped → Delivered (or Cancelled)
10. **Admin workflow**: receive paid order → download photos ZIP → print → enter AWB → mark shipped

## Order Number Format

`FT-YYYYNNNN` — e.g., `FT-20260001` (year + sequential counter)

## Currency

- All prices in RON (Romanian Lei)
- Format: `XX,XX RON` (comma as decimal separator)
- Shipping: Easybox = 20 RON, Courier = 25 RON (configurable)

## Roles

- **Customer**: default role; can upload, order, view own orders
- **Admin**: can manage all orders, products, and view analytics
- **Guest**: anonymous users with limited session (7 days)

## File Structure

```
PhotoPrint.sln
src/
  PhotoPrint.API/          → Backend API
  PhotoPrint.Tests/        → xUnit tests
photo-print-fe/            → Angular frontend
docker-compose.yml         → PostgreSQL + API + MailHog
docs/
  stories/                 → User story instruction files
  IMPLEMENTATION_ORDER.md  → Build sequence guide
.github/
  skills/                  → Copilot agent skills
```

## External Integrations

| Service | Purpose | Phase |
|---------|---------|-------|
| Stripe | International card payments | MVP |
| EuPlatesc | Romanian card payments | MVP |
| Google OAuth | Social login | MVP |
| Sameday API | Locker list + AWB generation | Phase 2 |
| SendGrid | Production email delivery | MVP |
| MailHog | Development email testing | Dev only |

## Data Model Quick Reference

- **Users**: accounts with email, password hash, role
- **GuestSessions**: temporary anonymous sessions (7-day TTL)
- **Products**: print format+finish combinations with prices
- **Uploads**: photo files with dimensions metadata
- **Orders**: payment + shipping + status tracking
- **OrderItems**: individual photos in an order with quantity
- **EasyboxLockers**: Sameday locker locations with coordinates

## Order Status State Machine

Valid transitions:
- `AwaitingPayment` → `Paid` (webhook only)
- `AwaitingPayment` → `PaymentFailed` (webhook only)
- `Paid` → `Printing` (admin)
- `Printing` → `Shipped` (admin + AWB)
- `Shipped` → `Delivered` (admin)
- `Paid`/`Printing` → `Cancelled` (admin + refund)
- All other transitions → 400 `Tranziție de status invalidă`
