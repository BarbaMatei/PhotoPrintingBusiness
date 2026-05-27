# Tech Stack

## Overview
FotoTipar is a full-stack web application for photo printing e-commerce, built with Angular 17+ on the frontend and ASP.NET Core 8 on the backend, targeting Romanian customers.

## Languages

**Frontend**: TypeScript 5.x (strict mode)
**Backend**: C# 12 (.NET 8)

TypeScript provides type safety for the Angular SPA. C# with ASP.NET Core 8 provides a robust, high-performance backend with excellent tooling for enterprise web APIs.

## Framework

**Frontend**: Angular 17+ (standalone components, lazy-loaded feature modules)
**Backend**: ASP.NET Core 8 Web API

Angular was chosen for its opinionated structure, built-in dependency injection, reactive forms, and strong TypeScript integration. ASP.NET Core 8 was chosen for its performance, mature ecosystem (EF Core, SignalR, FluentValidation), and first-class support for REST APIs.

### Key Frontend Libraries
- `@stripe/stripe-js` — Stripe Elements for PCI-compliant card payments
- `leaflet` + `@types/leaflet` — maps for Easybox locker selection
- `ng2-charts` (Chart.js) — admin dashboard charts
- `@microsoft/signalr` — real-time admin order notifications
- `heic2any` — HEIC image preview conversion in browser

### Key Backend Libraries
- `Npgsql.EntityFrameworkCore.PostgreSQL` — EF Core PostgreSQL provider
- `FluentValidation` — request DTO validation
- `Serilog` — structured logging
- `Stripe.net` — Stripe payment integration
- `MailKit` (dev) / `SendGrid` (prod) — email delivery

## Authentication

**Strategy**: JWT RS256 + Google OAuth + Guest Tokens

- JWT RS256: 15-min access token, 30-day refresh token (HttpOnly cookie, SHA-256 hashed in DB, rotated on use)
- Google OAuth: via Google Identity Services; backend verifies `id_token` and issues own JWT
- Guest sessions: `X-Guest-Token` header, 7-day TTL, can be claimed after registration
- Dual auth: endpoints accept EITHER Bearer JWT OR X-Guest-Token

## Infrastructure & Deployment

**Development**: Docker Compose (PostgreSQL 16 + API + MailHog)
**Production**: Docker container on VPS/cloud (Azure App Service or DigitalOcean)
**Frontend hosting**: Static files on CDN (Vercel, Netlify, or Nginx)
**Database hosting**: Managed PostgreSQL (Azure Database for PostgreSQL or DigitalOcean Managed DB)
**File storage**: Local disk initially → S3/Azure Blob via `IStorageService` abstraction
**Reverse proxy**: Nginx or Caddy for HTTPS termination (Let's Encrypt)
**CI/CD**: GitHub Actions (lint, test, build on PR; deploy on merge to main)

## Package Manager

**Frontend**: npm (Angular default)
**Backend**: NuGet (dotnet restore)

## Decision Relationships
- Angular's TypeScript-first design pairs naturally with strict typing in the frontend
- ASP.NET Core + EF Core + PostgreSQL is a well-established full-stack pattern for .NET projects
- SignalR (built into ASP.NET Core) provides real-time admin notifications without additional infrastructure
- JWT + refresh token rotation follows OWASP best practices for stateless authentication
