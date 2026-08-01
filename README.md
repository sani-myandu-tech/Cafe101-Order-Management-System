<div align="center">

# Café 101

### Point-of-Sale & Café Management System

A role-based desktop POS system built on a layered architecture, with a static data-access gateway over SQL Server.

`C#` · `.NET Framework 4.8` · `WinForms` · `SQL Server` · `Microsoft.Data.SqlClient`

</div>

---

## Overview

Café 101 is a desktop point-of-sale and café management system built for a live, multi-role operating environment: cashiers taking orders, kitchen staff managing preparation, managers overseeing staff and stock, and an owner tracking business performance. It was built as a university capstone project (ISTN3AS) and designed to run against a real, persistent SQL Server database rather than a mock dataset.

The system's core design goal was **separation of concerns between UI, business rules, and data access** — every form talks to the database through a single static gateway class rather than embedding SQL throughout the UI layer.

---

## Architecture

The application is structured in three layers:

```
┌────────────────────────────────────────────────┐
│                PRESENTATION LAYER              │
│  Cashier · HeadChef · Manager · OwnerDashboard │
│         Form1 (Login) · ResetPassword          │
│              WinForms + GDI+ (SplashScreen)    │
└───────────────────────┬────────────────────────┘
                        │
┌───────────────────────▼────────────────────────┐
│               DATA ACCESS LAYER                │
│                 DatabaseHelper.cs              │
│   Static gateway — all SQL lives here, exposed │
│   as typed, parameterized methods per domain:  │
│   Auth · Staff · Customers · Menu · Orders ·   │
│   Suppliers · Purchase Orders · Reports        │
└───────────────────────┬────────────────────────┘
                        │
┌───────────────────────▼────────────────────────┐
│                 PERSISTENCE LAYER              │
│                    SQL Server                  │
│   Users · Customers · Categories · MenuItems · │
│   Suppliers · Orders · OrderItems ·            │
│   PurchaseOrders                               │
└────────────────────────────────────────────────┘
```

**Why this shape:** WinForms code-behind classes (`Cashier.cs`, `Manager.cs`, etc.) never construct SQL directly — they call typed methods on `DatabaseHelper` (e.g. `GetMenuItems()`, `CreateOrder(...)`, `GetLowStockItems()`) and receive back `DataTable`s or primitives. This keeps every SQL statement centralized, parameterized, and auditable in one file, and means a form's responsibility is limited to presentation and user interaction — not query construction.

### Design patterns in use

| Pattern | Where | Why |
|---|---|---|
| **Table/Gateway pattern (static)** | `DatabaseHelper` | Single, centralized access point per table/domain; avoids scattering SQL and connection lifecycle management across the UI layer |
| **Role-based State pattern (implicit)** | Login → role-specific dashboard routing | The application's behavior and available UI change based on `CurrentUserRole`, effectively branching into distinct states post-authentication |
| **Composite UI structure** | Manager / Owner dashboards | Dashboards are composed of independently updatable sub-views (KPI panels, tables, charts) driven by the same underlying data methods |

---

## Key Features

- **Role-based dashboards** — Cashier, Head Chef, Manager, and Owner each get a purpose-built interface and permission set
- **Order lifecycle management** — orders move through `Pending → Preparing → Ready → Served`, with cancellation support
- **Inventory & menu control** — stock levels tracked per item, automatic low-stock flagging via `MinStockQty` thresholds
- **Customer relationship records** — searchable customer database with contact info and freeform notes
- **Supplier & procurement workflow** — supplier records feed into purchase orders, which restock inventory on receipt
- **Owner analytics dashboard** — daily/weekly/monthly/all-time revenue, top and least-selling items, revenue breakdown by payment method, and arbitrary date-range KPI filtering
- **Security** — SHA-256 password hashing (no plaintext credentials in the database), parameterized queries throughout (no string-concatenated SQL, no injection surface)
- **Custom splash screen** — GDI+-rendered animated startup sequence

---

## Tech Stack

| Layer | Technology |
|---|---|
| UI | WinForms, GDI+ |
| Language / Runtime | C#, .NET Framework 4.8 |
| Data Access | `Microsoft.Data.SqlClient` |
| Configuration | `System.Configuration.ConfigurationManager` |
| Database | Microsoft SQL Server |

---

## Getting Started

### Prerequisites
- Visual Studio 2022+ with the **.NET desktop development** workload
- Access to a SQL Server instance (local or remote)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/sani-myandu-tech/Cafe101-mm.git
   ```

2. **Open** `Cafe101.sln` in Visual Studio.

3. **Configure the database connection.**
   `App.config` is intentionally excluded from source control (see `.gitignore`) — credentials never live in the repository. To set up locally:
   ```bash
   cp Cafe101/App.config.example Cafe101/App.config
   ```
   Then edit `Cafe101/App.config` and fill in your SQL Server connection details under the `Cafe101Db` connection string.

4. **Provision the schema.** Either:
   - Run `Cafe101_Database_Setup.sql` directly against your SQL Server instance, or
   - Let the application create and seed tables automatically on first run via `DatabaseHelper.CreateDatabaseIfNeeded()`.

5. **Build and run** (F5 in Visual Studio).

> **Security note:** Never commit a filled-in `App.config` to version control. The `.gitignore` in this repo already excludes it — keep it that way if you fork or extend this project.

---

## Project Structure

```
Cafe101_System/
├── Cafe101.sln
├── Cafe101_Database_Setup.sql       # Full schema + seed data
├── Cafe101_Manual_Database.sql      # Manual/reference DB script
├── Cafe101_HelpGuide.docx           # End-user help guide
└── Cafe101/
    ├── DatabaseHelper.cs            # Data access layer — all SQL lives here
    ├── Form1.cs                     # Login
    ├── ResetPassword.cs             # Password reset flow
    ├── Cashier.cs                   # Cashier dashboard
    ├── HeadChef.cs                  # Head Chef dashboard
    ├── Manager.cs                   # Manager dashboard
    ├── OwnerDashboard.cs            # Owner analytics dashboard
    ├── SplashScreen.cs              # GDI+ animated startup screen
    ├── App.config.example           # Connection string template
    └── Resources/                   # Menu item imagery
```

---

## Database Schema (summary)

| Table | Purpose |
|---|---|
| `Users` | Staff accounts, role assignment, hashed credentials |
| `Customers` | Customer contact records and notes |
| `Categories` / `MenuItems` | Menu structure, pricing, stock thresholds |
| `Suppliers` | Vendor contact and account details |
| `Orders` / `OrderItems` | Order headers and line items, with computed subtotal columns |
| `PurchaseOrders` | Procurement requests linked to suppliers, feeding stock replenishment |

Full DDL, constraints, and seed data are in `Cafe101_Database.sql`.

---

## Author

**Lungisani Mnyandu**
Final-year Information Systems student, University of KwaZulu-Natal
[GitHub](https://github.com/sani-myandu-tech) · [LinkedIn](https://www.linkedin.com/in/lungisani-mnyandu)

## License
ISTN3AS module, UKZN. Not licensed for commercial use.
