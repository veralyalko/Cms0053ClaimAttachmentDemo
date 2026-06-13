# Cms0053ClaimAttachmentDemo

## Project Purpose

This is a functional prototype demonstrating how a payer can support CMS-0053-F compliant claim attachment workflows. It is intended as a demo/reference implementation for understanding the workflow mechanics, not as production software. All data is fake. No real PHI is used.

---

## CMS-0053-F Alignment

CMS-0053-F establishes requirements for electronic attachment submission with healthcare claims. Key standards involved in production implementations:

- **X12 275** — Claim attachment transaction (provider → payer)
- **X12 277** — Payer request for additional information (payer → provider)
- **X12 837** — Original claim transaction (used for claim linkage via ICN)

This prototype simulates these workflows using form-based submissions and a mock clearinghouse. Every point where a real X12 transaction would be generated or parsed is marked with a placeholder comment (see §X12 Integration Points below).

---

## Required Workflows

### Workflow 1 — Solicited Attachment
1. Payer has a claim and determines more documentation is needed.
2. Payer creates an `AttachmentRequest` (X12 277 proxy), generating a secure upload token.
3. Provider opens the secure upload link and uploads the requested document.
4. The shared payer-side processing pipeline runs.

### Workflow 2 — Unsolicited Attachment via Clearinghouse
1. Provider submits an attachment to the mock clearinghouse.
2. Mock clearinghouse stores the record with status `New`.
3. Payer triggers a pull from the clearinghouse (button on the dashboard).
4. Each pulled attachment is fed into the shared payer-side processing pipeline.

### Workflow 3 — Unsolicited Direct EMR/Provider Submission
1. Provider or EMR user navigates to the direct submit page.
2. They can select a seeded fake EMR document or upload their own file.
3. They fill in metadata (NPI, patient name, service date, document type).
4. The shared payer-side processing pipeline runs.

---

## Technology Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core, Razor Pages |
| ORM | Entity Framework Core |
| Database | SQLite |
| UI | Bootstrap 5 |
| File storage | Local disk (`wwwroot/uploads/`) |
| Language | C# |

---

## Database Entities

### Claim
Seeded. Represents existing payer claims for matching against.

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| ClaimNumber | string | e.g. CLM-2025-0001 |
| PatientName | string | fake name |
| PatientDOB | DateOnly | |
| ProviderNPI | string | 10-digit mock NPI |
| ProviderName | string | |
| ServiceDate | DateOnly | |
| DiagnosisCode | string | e.g. "M54.5" |
| AmountBilled | decimal | |
| Status | string | Open / Pending / Closed |

### AttachmentRequest
Payer-generated request for documentation. X12 277 proxy.

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| ClaimId | int | FK → Claim |
| TrackingNumber | string | GUID-based, unique |
| DocumentTypeRequested | string | e.g. "Operative Report" |
| RequestReason | string | free text |
| RequestedAt | DateTime | |
| DueDate | DateTime | |
| ProviderEmail | string | where secure link is "sent" |
| SecureUploadToken | string | GUID, used in upload URL |
| Status | string | Pending / Fulfilled / Expired / Cancelled |

### ClaimAttachment
Core entity. Created by all three workflows.

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| ClaimId | int? | FK → Claim, null until matched |
| AttachmentRequestId | int? | FK → AttachmentRequest, null for unsolicited |
| SourceType | string | Solicited / UnsolicitedClearinghouse / UnsolicitedDirect |
| FileName | string | original filename |
| StoredFileName | string | GUID-based name on disk |
| FileSizeBytes | long | |
| ContentType | string | MIME type |
| FileHash | string | SHA-256 hex |
| SubmittedBy | string | provider name or NPI |
| SubmittedAt | DateTime | |
| PatientName | string | from submission metadata |
| PatientDOB | DateOnly? | |
| ProviderNPI | string | |
| ServiceDate | DateOnly | |
| DocumentType | string | |
| Notes | string? | |
| Status | string | see Statuses section |
| CreatedAt | DateTime | |

### AttachmentStatusHistory
Immutable audit trail. One row per status transition.

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| ClaimAttachmentId | int | FK |
| FromStatus | string? | null for first entry |
| ToStatus | string | |
| Notes | string? | |
| ChangedAt | DateTime | |
| ChangedBy | string | "System" or actor label |

### AttachmentValidationResult
One record per attachment. Created by the validation step of the pipeline.

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| ClaimAttachmentId | int | FK, unique |
| IsValid | bool | |
| Errors | string | JSON array of strings |
| Warnings | string | JSON array of strings |
| ValidatedAt | DateTime | |

### ClearinghouseAttachment
Mock clearinghouse staging table. Populated by provider submissions (Workflow 2).

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| TrackingNumber | string | |
| ProviderNPI | string | |
| PatientName | string | |
| PatientDOB | DateOnly? | |
| ServiceDate | DateOnly | |
| DocumentType | string | |
| FileName | string | original filename |
| StoredFileName | string | GUID-based name on disk |
| FileSizeBytes | long | |
| ContentType | string | |
| SubmittedAt | DateTime | |
| Status | string | New / Pulled |
| PulledAt | DateTime? | |

### EmrDocument
Seeded fake EMR documents for use in Workflow 3.

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| DocumentName | string | display name |
| DocumentType | string | e.g. "Lab Results" |
| PatientName | string | matches a seeded Claim |
| PatientDOB | DateOnly | |
| ProviderNPI | string | |
| ServiceDate | DateOnly | |
| FileName | string | static file under wwwroot/emr-samples/ |

---

## Statuses

### ClaimAttachment Status Flow

```
Received → Processing → Validated ──→ Matched   → Accepted
                      ↘ ValidationFailed  ↘ MatchFailed → (manual review)
                                                        → Rejected
```

| Status | Meaning |
|---|---|
| Received | Attachment has arrived, record created |
| Processing | Pipeline is running |
| Validated | File and metadata passed all validation rules |
| ValidationFailed | One or more validation errors |
| Matched | Linked to a Claim record |
| MatchFailed | No matching Claim found |
| Accepted | Fully processed and accepted by payer |
| Rejected | Rejected for a business reason |

### AttachmentRequest Status Flow

```
Pending → Fulfilled | Expired | Cancelled
```

---

## Shared Payer-Side Processing Pipeline

All three workflows call `AttachmentProcessingService.ProcessAsync()`. Steps run in order:

1. **Create record** — insert `ClaimAttachment` with status `Received`
2. **Store file** — copy file to `wwwroot/uploads/{guid}{ext}` via `FileStorageService`
3. **Calculate hash** — compute SHA-256 of stored file bytes
4. **Validate** — run all validation rules, write `AttachmentValidationResult`
5. **Match claim** — run `ClaimMatchingService`, set `ClaimId` if found
6. **Save validation result** — persist errors/warnings and `IsValid` flag
7. **Update status** — set final `ClaimAttachment.Status` based on validation + match outcome
8. **Write status history** — append a row to `AttachmentStatusHistory` for each transition
9. **Return result** — return `ProcessingResult` with final status, errors, matched claim info

If step 4 produces errors, the pipeline still runs steps 5–9 so the record is always fully persisted with a complete audit trail.

---

## Validation Rules

### File Checks
- File must not be empty (size > 0 bytes)
- File size must be ≤ 10 MB
- Allowed MIME types: `application/pdf`, `image/jpeg`, `image/png`, `image/tiff`, `text/plain`
- Filename must not be blank

### Metadata Checks
- `ProviderNPI` must be exactly 10 digits
- `PatientName` must not be blank
- `ServiceDate` must not be in the future
- `ServiceDate` must be within the last 2 years (demo timely-filing rule)
- `DocumentType` must not be blank

Errors block acceptance. Warnings are informational only.

---

## Claim Matching Rules

`ClaimMatchingService.MatchAsync()` queries the `Claims` table with these criteria:

1. `ProviderNPI` must match exactly — **required**
2. `PatientName` must match case-insensitively after trimming — **required**
3. `ServiceDate` must be within ±7 days of the claim's service date — **required**
4. `PatientDOB`, if provided on the attachment, must match — optional tiebreaker

**Result logic:**
- One match → set `ClaimAttachment.ClaimId`, status → `Matched`
- Multiple matches → use closest service date; add a warning
- No match → status → `MatchFailed`, flagged for manual review

---

## X12 Integration Points

Every placeholder is marked in code with a comment tag so they are easy to find with grep.

| Tag | Location | What replaces it in production |
|---|---|---|
| `[X12-277-PLACEHOLDER]` | `Requests/Create.cshtml.cs` | Generate and transmit an X12 277 Additional Information Request instead of saving a DB record |
| `[X12-275-PLACEHOLDER]` | `AttachmentProcessingService.cs` | Parse an incoming X12 275 Transaction Set envelope to extract metadata and binary payload instead of reading form fields |
| `[X12-275-CLEARINGHOUSE-PLACEHOLDER]` | `MockClearinghouseService.cs` | Replace mock DB query with real clearinghouse API or SFTP retrieval of X12 275 transactions |
| `[X12-837-LINKAGE-PLACEHOLDER]` | `ClaimMatchingService.cs` | Supplement name/NPI matching with the X12 837 Internal Control Number (ICN) for exact claim linkage |
| `[SCHEMATRON-PLACEHOLDER]` | `AttachmentProcessingService.cs` | Replace hand-coded LINQ-to-XML assertions with compiled HL7 C-CDA Schematron XSLT execution (Saxon-HE) against the full official rule set |
| `[NPPES-PLACEHOLDER]` | `AttachmentProcessingService.cs` | Replace mock dictionary lookup with HTTP call to `https://npiregistry.cms.hhs.gov/api/?number={npi}&version=2.1` to verify NPI exists and is actively enrolled |

---

## Coding Rules

- No real PHI. All patient names, DOBs, NPIs, and claim numbers are fake.
- No real clearinghouse APIs.
- No real EMR APIs.
- No real X12 parsing (275, 277, 837).
- Do not over-engineer. No unnecessary abstractions, base classes, or generic helpers.
- No comments that describe what the code does. Only comment the non-obvious why, or X12 placeholder tags.
- No multi-paragraph docstrings.
- Prefer editing existing files over creating new ones.
- All three workflows must call the same `AttachmentProcessingService` pipeline — do not duplicate pipeline logic per workflow.
- Keep pages focused: each page demonstrates one step of one workflow.
- Bootstrap for all UI. No custom CSS frameworks.
- File storage is local disk only. No cloud storage.
- SQLite only. No connection string configuration needed for demo.

---

## Build Order

| Phase | What gets built |
|---|---|
| 1 — Foundation | `Program.cs`, `AppDbContext`, all Models, `SeedData`, EF migrations, `FileStorageService` |
| 2 — Shared Pipeline | `ClaimMatchingService`, `AttachmentProcessingService` |
| 3 — Workflow 1 (Solicited) | `Payer/Requests/Create`, `Payer/Requests/Index`, `Provider/Upload/{token}` |
| 4 — Workflow 2 (Clearinghouse) | `MockClearinghouseService`, `Clearinghouse/Submit`, pull button on Dashboard |
| 5 — Workflow 3 (Direct/EMR) | EMR seed documents, `Provider/DirectSubmit` |
| 6 — Payer Dashboard & Detail | `Payer/Dashboard`, `Payer/Attachments/Index`, `Payer/Attachments/Detail/{id}` |

---

## Prototype vs Production

| Area | This Prototype | Production Requirement |
|---|---|---|
| Claim attachment submission | HTML form upload | X12 275 Transaction Set over AS2/SFTP or REST |
| Payer information request | DB record + demo URL | X12 277 transmitted to provider/clearinghouse |
| Claim linkage | Name + NPI + date matching | X12 837 ICN, payer claim control number |
| Clearinghouse integration | Mock DB table | Real clearinghouse API (e.g. Availity, Change Healthcare) |
| EMR integration | Seeded fake documents | Real FHIR R4 DocumentReference or HL7 CDA |
| Authentication | None | OAuth 2.0 / SMART on FHIR for provider portal |
| File storage | Local `wwwroot/uploads/` | Encrypted cloud object storage (S3, Azure Blob) |
| Audit trail | `AttachmentStatusHistory` table | HIPAA-compliant audit log with retention policy |
| NPI validation | Regex (10 digits) | NPPES NPI Registry lookup |
| Timely filing | 2-year demo rule | Payer-specific timely filing limits per contract |
| PHI handling | Fake data only | Full HIPAA compliance, encryption at rest and in transit |
