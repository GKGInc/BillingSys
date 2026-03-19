# End-to-end test checklist

Run after local or production deploy.

## Prerequisites

- Azurite running **or** Azure Storage connection string configured.
- Function App running locally (Visual Studio F5) **or** deployed URL in `appsettings.json`.
- Entra + Google IdP configured; user has `@tech85.com` in token claims.

## Authentication

- [ ] Unauthenticated user hitting a protected route is redirected to login.
- [ ] Login with tech85 Google account succeeds.
- [ ] `Sign Out` returns to logged-out state without errors.

## API (sample: time entries)

- [ ] `GET /api/timeentries` returns 200 with valid token (check Network tab).
- [ ] `GET` without token returns 401.

## Billing workflow (incremental)

- [ ] Create or view a **time entry** (or seed data).
- [ ] **Projects** page loads without errors.
- [ ] **Weekly billing** / **Invoices** pages load (may be empty).

## Regression

- [ ] Browser console has no unhandled Blazor exceptions on main navigation.

Record failures with: URL, user, HTTP status, and Function App log snippet (Azure Portal → Functions → Log stream).
