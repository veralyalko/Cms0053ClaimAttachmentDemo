using System.Security.Cryptography;
using System.Text.Json;
using Cms0053ClaimAttachmentDemo.Models;

namespace Cms0053ClaimAttachmentDemo.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext db, string contentRootPath)
    {
        if (db.Claims.Any())
            return;

        var uploadsDir = Path.Combine(contentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsDir);
        var emrDir = Path.Combine(contentRootPath, "wwwroot", "emr-samples");

        // ── Claims ────────────────────────────────────────────────────────────

        var claims = new List<Claim>
        {
            new() { ClaimNumber = "CLM-2025-0001", PatientName = "John Smith",       PatientDOB = new DateOnly(1975,  3, 15), ProviderNPI = "1234567890", ProviderName = "Metro Health Clinic",       ServiceDate = new DateOnly(2025, 10, 15), DiagnosisCode = "M54.5",    AmountBilled = 350.00m,  Status = "Open",    CreatedAt = DateTime.UtcNow },
            new() { ClaimNumber = "CLM-2025-0002", PatientName = "Maria Garcia",      PatientDOB = new DateOnly(1982,  7, 22), ProviderNPI = "1234567890", ProviderName = "Metro Health Clinic",       ServiceDate = new DateOnly(2025, 11,  3), DiagnosisCode = "J45.909", AmountBilled = 175.00m,  Status = "Pending", CreatedAt = DateTime.UtcNow },
            new() { ClaimNumber = "CLM-2025-0003", PatientName = "Robert Johnson",    PatientDOB = new DateOnly(1960, 12,  1), ProviderNPI = "9876543210", ProviderName = "Riverside Family Medicine", ServiceDate = new DateOnly(2025,  9, 28), DiagnosisCode = "E11.9",   AmountBilled = 520.00m,  Status = "Open",    CreatedAt = DateTime.UtcNow },
            new() { ClaimNumber = "CLM-2025-0004", PatientName = "Linda Chen",        PatientDOB = new DateOnly(1990,  5, 14), ProviderNPI = "9876543210", ProviderName = "Riverside Family Medicine", ServiceDate = new DateOnly(2025, 12, 10), DiagnosisCode = "K21.0",   AmountBilled = 290.00m,  Status = "Pending", CreatedAt = DateTime.UtcNow },
            new() { ClaimNumber = "CLM-2025-0005", PatientName = "James Wilson",      PatientDOB = new DateOnly(1955,  8, 30), ProviderNPI = "5551234567", ProviderName = "Valley Orthopedic Group",   ServiceDate = new DateOnly(2025, 11, 20), DiagnosisCode = "M17.11",  AmountBilled = 1250.00m, Status = "Open",    CreatedAt = DateTime.UtcNow },
            new() { ClaimNumber = "CLM-2025-0006", PatientName = "Patricia Moore",    PatientDOB = new DateOnly(1948,  2, 17), ProviderNPI = "5551234567", ProviderName = "Valley Orthopedic Group",   ServiceDate = new DateOnly(2025, 12,  5), DiagnosisCode = "M48.06",  AmountBilled = 680.00m,  Status = "Open",    CreatedAt = DateTime.UtcNow },
            new() { ClaimNumber = "CLM-2025-0007", PatientName = "Sarah Thompson",    PatientDOB = new DateOnly(1988,  4, 12), ProviderNPI = "1234567890", ProviderName = "Metro Health Clinic",       ServiceDate = new DateOnly(2025, 10, 22), DiagnosisCode = "R51.9",   AmountBilled = 195.00m,  Status = "Open",    CreatedAt = DateTime.UtcNow },
            new() { ClaimNumber = "CLM-2025-0008", PatientName = "David Kim",         PatientDOB = new DateOnly(1972,  9,  3), ProviderNPI = "9876543210", ProviderName = "Riverside Family Medicine", ServiceDate = new DateOnly(2025, 11,  7), DiagnosisCode = "J06.9",   AmountBilled = 125.00m,  Status = "Open",    CreatedAt = DateTime.UtcNow },
            new() { ClaimNumber = "CLM-2025-0009", PatientName = "Amanda Foster",     PatientDOB = new DateOnly(1965,  1, 28), ProviderNPI = "5551234567", ProviderName = "Valley Orthopedic Group",   ServiceDate = new DateOnly(2025, 11, 25), DiagnosisCode = "M25.511", AmountBilled = 450.00m,  Status = "Pending", CreatedAt = DateTime.UtcNow },
            new() { ClaimNumber = "CLM-2025-0010", PatientName = "Richard Torres",    PatientDOB = new DateOnly(1980,  6, 15), ProviderNPI = "1234567890", ProviderName = "Metro Health Clinic",       ServiceDate = new DateOnly(2025, 12,  3), DiagnosisCode = "G43.909", AmountBilled = 280.00m,  Status = "Open",    CreatedAt = DateTime.UtcNow },
        };
        db.Claims.AddRange(claims);

        // ── EMR Documents ─────────────────────────────────────────────────────
        // C-CDA R2.1 XML documents — structurally valid, demo data only, no real PHI.

        var emrDocs = new List<EmrDocument>
        {
            new() { DocumentName = "Lab Results — John Smith (10/15/2025)",                    DocumentType = "Lab Results",            PatientName = "John Smith",     PatientDOB = new DateOnly(1975,  3, 15), ProviderNPI = "1234567890", ServiceDate = new DateOnly(2025, 10, 15), FileName = "lab_results_john_smith.xml" },
            new() { DocumentName = "Operative Report — Robert Johnson (09/28/2025)",           DocumentType = "Operative Report",       PatientName = "Robert Johnson", PatientDOB = new DateOnly(1960, 12,  1), ProviderNPI = "9876543210", ServiceDate = new DateOnly(2025,  9, 28), FileName = "operative_report_robert_johnson.xml" },
            new() { DocumentName = "X-Ray Report — James Wilson (11/20/2025)",                 DocumentType = "Radiology Report",       PatientName = "James Wilson",   PatientDOB = new DateOnly(1955,  8, 30), ProviderNPI = "5551234567", ServiceDate = new DateOnly(2025, 11, 20), FileName = "xray_report_james_wilson.xml" },
            new() { DocumentName = "Office Visit Notes — Maria Garcia (11/03/2025)",           DocumentType = "Office Visit Notes",     PatientName = "Maria Garcia",   PatientDOB = new DateOnly(1982,  7, 22), ProviderNPI = "1234567890", ServiceDate = new DateOnly(2025, 11,  3), FileName = "office_visit_maria_garcia.xml" },
            new() { DocumentName = "Physical Therapy Notes — Linda Chen (12/10/2025)",         DocumentType = "Physical Therapy Notes", PatientName = "Linda Chen",     PatientDOB = new DateOnly(1990,  5, 14), ProviderNPI = "9876543210", ServiceDate = new DateOnly(2025, 12, 10), FileName = "therapy_notes_linda_chen.xml" },
            new() { DocumentName = "Spine Consultation Note — Patricia Moore (12/05/2025)",    DocumentType = "Consultation Note",      PatientName = "Patricia Moore", PatientDOB = new DateOnly(1948,  2, 17), ProviderNPI = "5551234567", ServiceDate = new DateOnly(2025, 12,  5), FileName = "spine_consult_patricia_moore.xml" },
        };
        db.EmrDocuments.AddRange(emrDocs);
        db.SaveChanges();

        // ── Attachment Requests ───────────────────────────────────────────────

        var clm1  = claims[0];  // John Smith
        var clm2  = claims[1];  // Maria Garcia
        var clm3  = claims[2];  // Robert Johnson
        var clm5  = claims[4];  // James Wilson
        var clm7  = claims[6];  // Sarah Thompson
        var clm8  = claims[7];  // David Kim
        var clm9  = claims[8];  // Amanda Foster
        var clm10 = claims[9];  // Richard Torres

        var requests = new List<AttachmentRequest>
        {
            new() { ClaimId = clm7.Id,  TrackingNumber = "ATR-2025-0001", DocumentTypeRequested = "Office Visit Notes",     RequestReason = "Medical necessity review required for E&M services billed.",              RequestedAt = DateTime.UtcNow.AddDays(-5),  DueDate = DateTime.UtcNow.AddDays(9),  ProviderEmail = "billing@metrohealth.example",      SecureUploadToken = Guid.NewGuid().ToString("N"), Status = AttachmentRequestStatus.Pending   },
            new() { ClaimId = clm8.Id,  TrackingNumber = "ATR-2025-0002", DocumentTypeRequested = "Lab Results",            RequestReason = "Lab results required to support medical necessity for ordered tests.",    RequestedAt = DateTime.UtcNow.AddDays(-3),  DueDate = DateTime.UtcNow.AddDays(4),  ProviderEmail = "records@riversidemedical.example", SecureUploadToken = Guid.NewGuid().ToString("N"), Status = AttachmentRequestStatus.Pending   },
            new() { ClaimId = clm9.Id,  TrackingNumber = "ATR-2025-0003", DocumentTypeRequested = "Radiology Report",       RequestReason = "Radiology report required for imaging services billed on this claim.",    RequestedAt = DateTime.UtcNow.AddDays(-1),  DueDate = DateTime.UtcNow.AddDays(20), ProviderEmail = "records@valleyortho.example",      SecureUploadToken = Guid.NewGuid().ToString("N"), Status = AttachmentRequestStatus.Pending   },
            new() { ClaimId = clm5.Id,  TrackingNumber = "ATR-2025-0004", DocumentTypeRequested = "Operative Report",       RequestReason = "Operative report required to validate procedure billed.",                 RequestedAt = DateTime.UtcNow.AddDays(-18), DueDate = DateTime.UtcNow.AddDays(-4), ProviderEmail = "records@valleyortho.example",      SecureUploadToken = Guid.NewGuid().ToString("N"), Status = AttachmentRequestStatus.Expired   },
            new() { ClaimId = clm10.Id, TrackingNumber = "ATR-2025-0005", DocumentTypeRequested = "Consultation Note",      RequestReason = "Consultation note required for specialist referral services.",             RequestedAt = DateTime.UtcNow.AddDays(-10), DueDate = DateTime.UtcNow.AddDays(5),  ProviderEmail = "billing@metrohealth.example",      SecureUploadToken = Guid.NewGuid().ToString("N"), Status = AttachmentRequestStatus.Cancelled },
        };
        db.AttachmentRequests.AddRange(requests);
        db.SaveChanges();

        // ── Clearinghouse Attachments (queued, ready to pull) ─────────────────

        var chFiles = new[]
        {
            ("lab_results_john_smith.xml",           "text/xml", "CLH-2025-0001", "1234567890", "John Smith",     new DateOnly(1975, 3, 15),  new DateOnly(2025, 10, 15), "Lab Results"),
            ("office_visit_maria_garcia.xml",        "text/xml", "CLH-2025-0002", "1234567890", "Maria Garcia",   new DateOnly(1982, 7, 22),  new DateOnly(2025, 11,  3), "Office Visit Notes"),
            ("operative_report_robert_johnson.xml",  "text/xml", "CLH-2025-0003", "9876543210", "Robert Johnson", new DateOnly(1960, 12, 1),  new DateOnly(2025,  9, 28), "Operative Report"),
        };

        foreach (var (emrFile, mime, tracking, npi, patient, dob, svcDate, docType) in chFiles)
        {
            var (stored, size, hash) = CopyToUploads(emrDir, uploadsDir, emrFile);
            db.ClearinghouseAttachments.Add(new ClearinghouseAttachment
            {
                TrackingNumber = tracking,
                ProviderNPI    = npi,
                PatientName    = patient,
                PatientDOB     = dob,
                ServiceDate    = svcDate,
                DocumentType   = docType,
                FileName       = emrFile,
                StoredFileName = stored,
                FileSizeBytes  = size,
                ContentType    = mime,
                SubmittedAt    = DateTime.UtcNow.AddDays(-2),
                Status         = ClearinghouseAttachmentStatus.New,
            });
        }
        db.SaveChanges();

        // ── Pre-processed Claim Attachments ───────────────────────────────────
        // Five records demonstrating each major pipeline outcome.

        SeedAttachment(db, uploadsDir, emrDir,
            sourceType:   AttachmentSourceType.Solicited,
            emrFile:      "lab_results_john_smith.xml",
            claimId:      clm1.Id,
            requestId:    requests[0].Id,
            patient:      "John Smith",      dob: new DateOnly(1975,  3, 15),
            npi:          "1234567890",      svcDate: new DateOnly(2025, 10, 15),
            docType:      "Lab Results",     submittedBy: "Metro Health Billing",
            daysAgo:      12,
            finalStatus:  AttachmentStatus.Accepted,
            isValid:      true,
            errors:       [],
            warnings:     [],
            history:
            [
                (null,                              AttachmentStatus.Received,         "Attachment received via solicited upload link"),
                (AttachmentStatus.Received,         AttachmentStatus.Processing,       "Pipeline started: validating file and metadata"),
                (AttachmentStatus.Processing,       AttachmentStatus.Validated,        "All validation checks passed"),
                (AttachmentStatus.Validated,        AttachmentStatus.Matched,          "Matched to claim CLM-2025-0001"),
                (AttachmentStatus.Matched,          AttachmentStatus.Accepted,         "Attachment accepted"),
            ]);

        SeedAttachment(db, uploadsDir, emrDir,
            sourceType:   AttachmentSourceType.UnsolicitedClearinghouse,
            emrFile:      "therapy_notes_linda_chen.xml",
            claimId:      clm2.Id,
            requestId:    null,
            patient:      "Maria Garcia",    dob: new DateOnly(1982,  7, 22),
            npi:          "1234567890",      svcDate: new DateOnly(2025, 11,  3),
            docType:      "Office Visit Notes", submittedBy: "Clearinghouse (NPI: 1234567890)",
            daysAgo:      8,
            finalStatus:  AttachmentStatus.Accepted,
            isValid:      true,
            errors:       [],
            warnings:     ["Potential duplicate: an accepted Office Visit Notes for this patient, provider NPI, and service date already exists (Attachment #1)."],
            history:
            [
                (null,                              AttachmentStatus.Received,         "Pulled from mock clearinghouse (tracking: CLH-2025-DEMO)"),
                (AttachmentStatus.Received,         AttachmentStatus.Processing,       "Pipeline started: validating file and metadata"),
                (AttachmentStatus.Processing,       AttachmentStatus.Validated,        "All validation checks passed"),
                (AttachmentStatus.Validated,        AttachmentStatus.Matched,          "Matched to claim CLM-2025-0002"),
                (AttachmentStatus.Matched,          AttachmentStatus.Accepted,         "Attachment accepted"),
            ]);

        SeedAttachment(db, uploadsDir, emrDir,
            sourceType:   AttachmentSourceType.UnsolicitedDirect,
            emrFile:      "xray_report_james_wilson.xml",
            claimId:      null,
            requestId:    null,
            patient:      "James Wilson",    dob: new DateOnly(1955,  8, 30),
            npi:          "5551234567",      svcDate: new DateOnly(2025, 11, 20),
            docType:      "Radiology Report", submittedBy: "Valley Imaging Center",
            daysAgo:      6,
            finalStatus:  AttachmentStatus.MatchFailed,
            isValid:      true,
            errors:       [],
            warnings:     [],
            history:
            [
                (null,                              AttachmentStatus.Received,         "Attachment received via direct provider submission"),
                (AttachmentStatus.Received,         AttachmentStatus.Processing,       "Pipeline started: validating file and metadata"),
                (AttachmentStatus.Processing,       AttachmentStatus.Validated,        "All validation checks passed"),
                (AttachmentStatus.Validated,        AttachmentStatus.MatchFailed,      "No claim found matching NPI + patient name + service date (±7 days)"),
            ]);

        SeedAttachment(db, uploadsDir, emrDir,
            sourceType:   AttachmentSourceType.UnsolicitedDirect,
            emrFile:      "spine_consult_patricia_moore.xml",
            claimId:      null,
            requestId:    null,
            patient:      "Richard Torres",  dob: new DateOnly(1980,  6, 15),
            npi:          "1234567890",      svcDate: new DateOnly(2025, 12,  3),
            docType:      "Consultation Note", submittedBy: "Provider Portal",
            daysAgo:      4,
            finalStatus:  AttachmentStatus.ValidationFailed,
            isValid:      false,
            errors:       ["C-CDA identity: patient name in document ('Patricia Moore') does not match submitted patient name ('Richard Torres')."],
            warnings:     [],
            history:
            [
                (null,                              AttachmentStatus.Received,         "Attachment received via direct provider submission"),
                (AttachmentStatus.Received,         AttachmentStatus.Processing,       "Pipeline started: validating file and metadata"),
                (AttachmentStatus.Processing,       AttachmentStatus.ValidationFailed, "1 validation error(s) found"),
            ]);

        SeedAttachment(db, uploadsDir, emrDir,
            sourceType:   AttachmentSourceType.UnsolicitedClearinghouse,
            emrFile:      "operative_report_robert_johnson.xml",
            claimId:      clm3.Id,
            requestId:    null,
            patient:      "Robert Johnson",  dob: new DateOnly(1960, 12,  1),
            npi:          "9876543210",      svcDate: new DateOnly(2025,  9, 28),
            docType:      "Operative Report", submittedBy: "Clearinghouse (NPI: 9876543210)",
            daysAgo:      15,
            finalStatus:  AttachmentStatus.Rejected,
            isValid:      true,
            errors:       [],
            warnings:     [],
            history:
            [
                (null,                              AttachmentStatus.Received,         "Pulled from mock clearinghouse (tracking: CLH-2025-HIST)"),
                (AttachmentStatus.Received,         AttachmentStatus.Processing,       "Pipeline started: validating file and metadata"),
                (AttachmentStatus.Processing,       AttachmentStatus.Validated,        "All validation checks passed"),
                (AttachmentStatus.Validated,        AttachmentStatus.Matched,          "Matched to claim CLM-2025-0003"),
                (AttachmentStatus.Matched,          AttachmentStatus.Accepted,         "Attachment accepted"),
                (AttachmentStatus.Accepted,         AttachmentStatus.Rejected,         "Rejected by payer: duplicate of previously submitted operative report on file"),
            ]);

        db.SaveChanges();
    }

    private static void SeedAttachment(
        AppDbContext db,
        string uploadsDir,
        string emrDir,
        string sourceType,
        string emrFile,
        int? claimId,
        int? requestId,
        string patient,
        DateOnly dob,
        string npi,
        DateOnly svcDate,
        string docType,
        string submittedBy,
        int daysAgo,
        string finalStatus,
        bool isValid,
        List<string> errors,
        List<string> warnings,
        (string? From, string To, string Notes)[] history)
    {
        var (stored, size, hash) = CopyToUploads(emrDir, uploadsDir, emrFile);
        var submittedAt = DateTime.UtcNow.AddDays(-daysAgo);

        var attachment = new ClaimAttachment
        {
            ClaimId             = claimId,
            AttachmentRequestId = requestId,
            SourceType          = sourceType,
            FileName            = emrFile,
            StoredFileName      = stored,
            FileSizeBytes       = size,
            ContentType         = "text/xml",
            FileHash            = hash,
            SubmittedBy         = submittedBy,
            SubmittedAt         = submittedAt,
            PatientName         = patient,
            PatientDOB          = dob,
            ProviderNPI         = npi,
            ServiceDate         = svcDate,
            DocumentType        = docType,
            Status              = finalStatus,
            CreatedAt           = submittedAt,
        };
        db.ClaimAttachments.Add(attachment);
        db.SaveChanges();

        db.AttachmentValidationResults.Add(new AttachmentValidationResult
        {
            ClaimAttachmentId = attachment.Id,
            IsValid           = isValid,
            Errors            = JsonSerializer.Serialize(errors),
            Warnings          = JsonSerializer.Serialize(warnings),
            ValidatedAt       = submittedAt.AddSeconds(2),
        });

        foreach (var (from, to, notes) in history)
        {
            db.AttachmentStatusHistories.Add(new AttachmentStatusHistory
            {
                ClaimAttachmentId = attachment.Id,
                FromStatus        = from,
                ToStatus          = to,
                Notes             = notes,
                ChangedAt         = submittedAt,
                ChangedBy         = "System",
            });
        }
    }

    private static (string StoredFileName, long Size, string Hash) CopyToUploads(
        string emrDir, string uploadsDir, string fileName)
    {
        var src = Path.Combine(emrDir, fileName);
        var ext = Path.GetExtension(fileName);
        var stored = $"{Guid.NewGuid():N}{ext}";
        var dest = Path.Combine(uploadsDir, stored);
        File.Copy(src, dest, overwrite: true);
        using var fs = File.OpenRead(dest);
        var hash = Convert.ToHexString(SHA256.HashData(fs)).ToLower();
        return (stored, new FileInfo(dest).Length, hash);
    }
}
