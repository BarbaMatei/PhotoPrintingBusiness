---
name: infer-workflows
description: >
  Infers business workflows from API routes, DB schema, and business logic files.
  Groups endpoints and models into named workflows, identifies the business domain,
  and flags incomplete or broken workflows.
  Run this skill after scan-architecture.
allowed-tools:
  - read_file
  - search_files
---

# Workflow Inference Engine

## How to infer workflows

You are reading code written by developers. Your job is to reverse-engineer the *business intent* from the technical implementation.

### Step 1 — Collect all routes/endpoints
Read every route/controller/resolver file. For each endpoint extract:
- HTTP method + path (or mutation/query name for GraphQL)
- Handler name
- Auth required (yes/no)
- Input shape (body/params/query)

### Step 2 — Collect all DB models/tables
From the schema scan, list every table/collection and its key fields.

### Step 3 — Cluster into workflows
Group the routes and models into business workflows using these rules:
- Routes sharing the same resource prefix (e.g. `/orders`, `/orders/:id`) belong to the same workflow
- Routes that reference the same DB table in their handlers belong to the same workflow
- A workflow typically has: create → read → update → delete + any state transitions

Name each workflow after the business concept, not the technical path.

Examples of workflow names:
- "User registration & authentication"
- "Order lifecycle management"
- "Payment processing"
- "Product catalogue management"
- "Inventory tracking"
- "Notification delivery"
- "Reporting & analytics"

### Step 4 — Detect the business domain
Based on the workflows you found, classify the system into one of:
- **E-commerce** — products, orders, cart, checkout, inventory
- **SaaS / B2B platform** — tenants, subscriptions, workspaces, billing
- **Fintech** — accounts, transactions, wallets, KYC, compliance
- **Healthcare** — patients, appointments, records, prescriptions
- **Logistics** — shipments, routes, tracking, delivery
- **Content platform** — posts, media, comments, feeds
- **Developer tools** — repos, pipelines, deployments
- **Other** — describe what you see

### Step 5 — Assess workflow completeness
For each workflow, mark it as:
- ✅ **Complete** — has create, read, update, delete and any required state transitions
- ⚠️ **Partial** — missing key steps (e.g. order workflow without payment step)
- ❌ **Stub** — route exists but handler is empty or returns `TODO`

Also flag:
- Workflows with no error handling in happy path routes
- Workflows missing notification/event emission after state changes
- Workflows with no audit trail (no created_by, updated_by, or event log)

## Output format

Produce a section titled **Inferred Business Workflows** with:

1. **Detected domain**: [domain name + 1 sentence explanation]
2. A table: `| Workflow | Endpoints | Key models | Status |`
3. For each workflow marked ⚠️ or ❌, one paragraph explaining what is missing and why it matters
