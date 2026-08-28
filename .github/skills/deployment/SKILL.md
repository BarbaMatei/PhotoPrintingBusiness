---
name: deployment
description: Deployment, Docker, CI/CD, and infrastructure configuration for FotoTipar. Use this skill when setting up Docker Compose, writing Dockerfiles, configuring GitHub Actions CI/CD, managing environment variables, or deploying to production.
---

## Development Environment

### Docker Compose

```yaml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: fototipar
      POSTGRES_USER: fototipar
      POSTGRES_PASSWORD: dev_password
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

  api:
    build: ./src/PhotoPrint.API
    ports:
      - "5001:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=fototipar;Username=fototipar;Password=dev_password
      - ASPNETCORE_ENVIRONMENT=Development
    depends_on:
      - postgres

  mailhog:
    image: mailhog/mailhog
    ports:
      - "1025:1025"  # SMTP
      - "8025:8025"  # Web UI

volumes:
  pgdata:
```

### Local Development Setup

1. `docker-compose up -d postgres mailhog` — start dependencies
2. `cd src/PhotoPrint.API && dotnet run` — start API
3. `cd photo-print-fe && ng serve` — start Angular dev server
4. API: `https://localhost:5001`
5. Frontend: `http://localhost:4200`
6. MailHog: `http://localhost:8025`

## Production Deployment

### Infrastructure

- **Backend**: Docker container on VPS or cloud (Azure App Service / DigitalOcean)
- **Frontend**: Static files on CDN (Vercel, Netlify, or Nginx)
- **Database**: Managed PostgreSQL (Azure Database for PostgreSQL / DigitalOcean Managed DB)
- **File Storage**: Local disk initially → migrate to S3/Azure Blob via `IStorageService`
- **Reverse Proxy**: Nginx or Caddy for HTTPS termination

### CI/CD Pipeline (GitHub Actions)

```yaml
# .github/workflows/ci.yml
name: CI
on: [push, pull_request]
jobs:
  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0' }
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build

  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '20' }
      - run: cd photo-print-fe && npm ci
      - run: cd photo-print-fe && npm run lint
      - run: cd photo-print-fe && npm test -- --watch=false --browsers=ChromeHeadless
      - run: cd photo-print-fe && npm run build -- --configuration=production
```

### Environment Variables (Production)

```
# Database
ConnectionStrings__DefaultConnection=Host=...;Database=fototipar;...;SslMode=Require

# JWT
Jwt__PrivateKeyPath=/etc/secrets/jwt-private.pem
Jwt__Issuer=fototipar.ro
Jwt__Audience=fototipar.ro

# Stripe
Stripe__SecretKey=sk_live_xxx
Stripe__WebhookSecret=whsec_xxx

# Google OAuth
Google__ClientId=xxx.apps.googleusercontent.com

# Email
Email__Provider=SendGrid
Email__SendGrid__ApiKey=SG.xxx
Email__FromAddress=noreply@fototipar.ro
Email__OperatorBcc=operator@fototipar.ro

# General
AllowedOrigins=https://fototipar.ro
ASPNETCORE_ENVIRONMENT=Production
```

### Database Migrations

- **Development**: `dotnet ef database update`
- **Production**: apply migrations at startup or via CI/CD pipeline
- **Rollback**: `dotnet ef database update <previous-migration>`
- Never drop tables in production — use data-preserving migrations

### SSL/TLS

- Use Let's Encrypt via Certbot or Caddy automatic HTTPS
- HSTS headers enabled in production
- All cookies: `Secure; SameSite=Strict`

### Monitoring

- Health check: `GET /health` — monitor with uptime service
- Serilog: structured logs to file with daily rotation
- Future: add Seq, ELK, or Application Insights for log aggregation

### Backup Strategy

- Database: daily automated backups (managed DB feature)
- Uploads: incremental backup to secondary storage
- Config: all in version control (except secrets)
