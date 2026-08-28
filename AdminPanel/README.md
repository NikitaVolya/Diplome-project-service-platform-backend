# ServiceHub — Web Admin Panel

ASP.NET Core 8 MVC back office for the ServiceHub platform (Software Developer 4 scope).
It reuses the existing `Domain` / `DAL` / `BLL` layers and runs as a **separate host** from the
mobile REST API, so the two can be deployed, scaled and secured independently.

---

## What is implemented

| Requirement | Where |
|---|---|
| Dashboard | `Controllers/DashboardController.cs`, `Views/Dashboard/Index.cshtml` |
| Statistics | `Controllers/StatisticsController.cs` — date range, daily breakdown, export |
| Users | list, filters, profile, block / unblock, soft delete, role assignment |
| Orders | list, filters, details, status change, delete (guarded by payments) |
| Complaints | list, details, resolve / reject, act on the reported user |
| Categories | two-level tree, create / edit, hide, delete (guarded by usage) |
| Reviews | list, rating filters, removal |
| Payments | list, totals by status, details, mark as refunded |
| Moderation | one queue with everything waiting for a decision |
| SignalR chat | `Hubs/AdminChatHub.cs`, `Views/Chat/Index.cshtml` |
| Admin UI | Bootstrap 5, custom `wwwroot/css/admin.css`, responsive sidebar |
| Charts | Chart.js, wrapped by `wwwroot/js/admin.js` (`AdminCharts.line/bar/doughnut`) |
| Role management | `Controllers/RolesController.cs` + per-user roles on the profile page |
| Logs viewer | `Controllers/LogsController.cs` over the `AuditLogs` table |
| Export to Excel | `BLL/Admin/Services/ExcelExportService.cs` — every list exports `.xlsx` |
| Responsive layout | sidebar collapses below 992 px, all tables scroll horizontally |

---

## Architecture

```
Domain            entities, enums, AppRoles constants
  DAL             ApplicationDbContext, repositories, migrations, DatabaseSeeder
    BLL           existing services + BLL/Admin/* (admin queries, audit, Excel)
      API         REST API for the mobile app          (JWT)
      AdminPanel  this project — MVC back office       (cookies)
```

The panel talks to the database through `BLL/Admin/*` services, which are the only place where
admin queries live. Controllers stay thin: bind the filter, call one service, render one view.

**Why a separate project.** The mobile API authenticates with bearer tokens; the panel needs
cookies, anti-forgery and Razor. Mixing both schemes in one host makes the security configuration
harder to reason about and couples the team's release cycles.

---

## Running it

1. **Connection string** — `appsettings.json` → `ConnectionStrings:DefaultConnection`.
   The default points at SQL Server LocalDB and the `KabanchikDB` database used by the API.

2. **Run** the `AdminPanel` project (F5 in Visual Studio, or `dotnet run --project AdminPanel`).

   On start-up the panel:
   - applies pending EF Core migrations (`Database:AutoMigrate`, default `true`);
   - creates the roles `Admin`, `Moderator`, `Support`, `Customer`, `Executor`;
   - creates the administrator account from the `Seed` section.

3. **Sign in** with the seeded account:

   ```
   admin@servicehub.local / Admin#2026
   ```

   Change `Seed:AdminPassword` before any real deployment.

### Demo data

`appsettings.Development.json` sets `Seed:DemoData: true`. On an **empty** database (no orders)
this generates categories, ~40 users, 160 orders with applications, payments, reviews, complaints
and chat threads, so the dashboard and charts are not empty during a demo. It also creates
`moderator@servicehub.local / Moderator#2026` and `support@servicehub.local / Support#2026`
so role-based access can be demonstrated. The seeder never touches a database that already
contains orders.

---

## Roles and permissions

| Section | Admin | Moderator | Support |
|---|:--:|:--:|:--:|
| Dashboard, Statistics, Orders, Chat | ✅ | ✅ | ✅ |
| Users, Categories, Complaints, Reviews, Moderation | ✅ | ✅ | — |
| Payments, Roles, Activity log | ✅ | — | — |
| Delete order / user, assign roles | ✅ | — | — |

Enforced by policies in `Extensions/ServiceCollectionExtensions.cs`
(`StaffOnly`, `Moderation`, `AdminOnly`). A fallback policy makes every page staff-only unless it
explicitly opts out, so a new controller cannot accidentally be published anonymously.

An account without a staff role is rejected at sign-in even if the password is correct — ordinary
platform users live in the same Identity store.

---

## Notable implementation details

**Blocking uses Identity lockout** (`LockoutEnd`) rather than a custom flag, so a blocked user is
also rejected by the mobile API's sign-in path. Deleting a user is a soft delete: orders, reviews
and payments keep valid foreign keys.

**Excel export has no third-party dependency.** `ExcelExportService` writes the OpenXML parts of a
workbook into a zip with `System.IO.Compression`. Strings are written inline instead of through a
shared-string table. Adding ClosedXML or EPPlus would have forced a new NuGet package on the whole
team for one feature.

**The activity log is append-only.** Every state-changing action writes an `AuditLog` row through
`AdminControllerBase.AuditAsync`. There is deliberately no delete action in the log viewer.
A failed write is swallowed: losing a log line must never fail the moderation decision that
triggered it.

**Charts are shaped on the server.** `BLL/Admin/Models/ChartData` (labels + series) is serialised
to JSON and handed to `AdminCharts`, so views contain no data massaging and the same payload feeds
`/Dashboard/ChartData` for the range selector.

**Sidebar badges are cached for 30 seconds** (`Services/NavigationBadgeService.cs`) because the
layout renders on every request and the counters need five aggregate queries.

---

## Adding a migration

A design-time factory (`DAL/Context/ApplicationDbContextFactory.cs`) is included, so migrations can
be added from the DAL folder without starting a host:

```bash
dotnet ef migrations add MyMigration --project DAL --startup-project AdminPanel
```

Override the design-time connection string with the `SERVICEHUB_CONNECTION` environment variable.

---

## Front-end assets

Bootstrap 5.3.3, Bootstrap Icons 1.11.3, Chart.js 4.4.3 and the SignalR JavaScript client 8.0.7 are
vendored into `wwwroot/lib/`. Nothing is loaded from a CDN, so the panel works — and demos —
without an internet connection.
