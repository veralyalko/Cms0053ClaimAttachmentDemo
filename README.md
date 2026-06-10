# CMS-0053-F Claim Attachment Demo

A functional prototype demonstrating how a payer can support CMS-0053-F compliant claim attachment workflows. Built with ASP.NET Core Razor Pages, Entity Framework Core, SQLite, and Bootstrap.

> **Prototype only.** All patient data is synthetic. No real X12 parsing is implemented. Do not use with real PHI.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) — installed via `brew install dotnet`
- No database setup required (SQLite, file created automatically on first run)

Verify your install:

```bash
dotnet --version
# Expected: 10.0.x
```

---

## Setup and Run

### 1. Add dotnet to your PATH (if needed)

If `dotnet` is not found after install, add it:

```bash
export PATH="/opt/homebrew/bin:$PATH"
```

To make this permanent, add the line to your `~/.zshrc`.

### 2. Restore dependencies

```bash
cd Cms0053ClaimAttachmentDemo
dotnet restore
```

### 3. Run the application

```bash
dotnet run
```

The terminal will print the URL. Open it in your browser:

```
http://localhost:5205
```

The SQLite database (`cms0053.db`) is created automatically on first run.

---

## Project Structure

```
Cms0053ClaimAttachmentDemo/
├── Data/
│   ├── AppDbContext.cs         EF Core DbContext
│   └── SeedData.cs             Seed claims + fake EMR documents (Phase 2)
├── Models/                     Entity classes (Phase 2)
├── Services/                   Processing pipeline and services (Phase 2)
├── Pages/
│   ├── Index.cshtml            Home page — workflow entry points
│   ├── Readiness.cshtml        CMS-0053 standards alignment checklist
│   ├── Payer/
│   │   ├── Dashboard.cshtml    All attachments and processing results
│   │   ├── Requests/           Workflow 1 — solicited attachment requests
│   │   └── Attachments/        All attachments detail view
│   ├── Provider/
│   │   └── DirectSubmit.cshtml Workflow 3 — direct/EMR submission
│   └── Clearinghouse/
│       └── Submit.cshtml       Workflow 2 — clearinghouse submission
└── wwwroot/
    └── uploads/                Local file storage for uploaded attachments
```

---

## Workflows

| # | Name | Entry Point |
|---|---|---|
| 1 | Solicited | `/Payer/Requests` |
| 2 | Unsolicited via Clearinghouse | `/Clearinghouse/Submit` |
| 3 | Unsolicited Direct / EMR | `/Provider/DirectSubmit` |

---

## Build Phases

| Phase | Status | What is built |
|---|---|---|
| 1 — Foundation | ✅ Done | Project scaffold, layout, home page, placeholder pages |
| 2 — Models & Pipeline | Pending | Entities, EF migrations, seed data, shared processing service |
| 3 — Workflow 1 | Pending | Solicited attachment request + provider upload |
| 4 — Workflow 2 | Pending | Clearinghouse submit + payer pull |
| 5 — Workflow 3 | Pending | Direct/EMR submission with seeded documents |
| 6 — Dashboard & Detail | Pending | Payer dashboard, attachment detail, status history |

---

## X12 Integration Points

Every location where a real X12 transaction would be generated or parsed is marked in code with a tag comment. Search for these tags to find the integration points:

| Tag | What it marks |
|---|---|
| `[X12-277-PLACEHOLDER]` | Where X12 277 request would be generated and transmitted |
| `[X12-275-PLACEHOLDER]` | Where X12 275 attachment would be parsed |
| `[X12-275-CLEARINGHOUSE-PLACEHOLDER]` | Where real clearinghouse retrieval would happen |
| `[X12-837-LINKAGE-PLACEHOLDER]` | Where X12 837 ICN would be used for exact claim matching |
