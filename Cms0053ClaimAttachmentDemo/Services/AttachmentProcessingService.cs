using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Cms0053ClaimAttachmentDemo.Data;
using Cms0053ClaimAttachmentDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace Cms0053ClaimAttachmentDemo.Services;

public record AttachmentProcessingInput(
    IFormFile File,
    string SourceType,
    string PatientName,
    DateOnly? PatientDOB,
    string ProviderNPI,
    DateOnly ServiceDate,
    string DocumentType,
    string SubmittedBy,
    string? Notes = null,
    int? AttachmentRequestId = null
);

public record ProcessingResult(
    bool Success,
    int ClaimAttachmentId,
    string FinalStatus,
    List<string> ValidationErrors,
    List<string> ValidationWarnings,
    Claim? MatchedClaim
);

public partial class AttachmentProcessingService(
    AppDbContext db,
    FileStorageService fileStorage,
    ClaimMatchingService claimMatcher)
{
    private static readonly HashSet<string> AllowedMimeTypes =
    [
        "application/pdf", "image/jpeg", "image/png", "image/tiff", "text/plain",
        "text/xml", "application/xml"
    ];

    private static readonly XNamespace CdaNs = "urn:hl7-org:v3";

    // LOINC codes that C-CDA <code> must carry for each submitted DocumentType label.
    private static readonly IReadOnlyDictionary<string, string[]> LoincByDocumentType =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Lab Results"]            = ["11502-2"],
            ["Operative Report"]       = ["11504-8"],
            ["Radiology Report"]       = ["18748-4"],
            ["Office Visit Notes"]     = ["34117-2"],
            ["Physical Therapy Notes"] = ["11514-3"],
            ["Consultation Note"]      = ["11488-4"],
        };

    // [NPPES-PLACEHOLDER] In production, replace with an HTTP call to the CMS NPPES NPI Registry:
    // GET https://npiregistry.cms.hhs.gov/api/?number={npi}&version=2.1
    // Parse the JSON result_count and basic_info.status fields to verify active enrollment.
    private static readonly IReadOnlyDictionary<string, string> MockNpiRegistry =
        new Dictionary<string, string>
        {
            ["1234567890"] = "Metro Health Clinic",
            ["9876543210"] = "Riverside Family Medicine",
            ["5551234567"] = "Valley Orthopedic Group",
        };

    // SHALL-conformance section assertions per document LOINC, mirroring C-CDA R2.1 Schematron rules.
    // [SCHEMATRON-PLACEHOLDER] In production, replace with compiled HL7 C-CDA Schematron XSLT (Saxon-HE).
    private static readonly IReadOnlyDictionary<string, (string Code, string Display)[]> RequiredSectionsByLoinc =
        new Dictionary<string, (string Code, string Display)[]>
        {
            ["11502-2"] = [("30954-2", "Results")],
            ["11504-8"] = [("29554-3", "Procedure Description")],
            ["18748-4"] = [("18782-3", "Radiology Study")],
            ["34117-2"] = [("10154-3", "Chief Complaint"), ("51847-2", "Assessment and Plan")],
            ["11514-3"] = [("61150-9", "Subjective"), ("61149-1", "Objective")],
            ["11488-4"] = [("10154-3", "Chief Complaint")],
        };

    // [X12-275-PLACEHOLDER] In production, this method accepts a parsed X12 275 Transaction Set
    // envelope instead of an IFormFile + metadata. The binary attachment and loop data segments
    // (2000A/2000B/2000C/2100/2200) map directly to the fields populated below.
    public async Task<ProcessingResult> ProcessAsync(AttachmentProcessingInput input)
    {
        var (storedFileName, fileSizeBytes) = await fileStorage.StoreFileAsync(input.File);

        var fileHash = await ComputeHashAsync(fileStorage.GetFilePath(storedFileName));

        var attachment = new ClaimAttachment
        {
            SourceType          = input.SourceType,
            FileName            = input.File.FileName,
            StoredFileName      = storedFileName,
            FileSizeBytes       = fileSizeBytes,
            ContentType         = input.File.ContentType,
            FileHash            = fileHash,
            SubmittedBy         = input.SubmittedBy,
            SubmittedAt         = DateTime.UtcNow,
            PatientName         = input.PatientName,
            PatientDOB          = input.PatientDOB,
            ProviderNPI         = input.ProviderNPI,
            ServiceDate         = input.ServiceDate,
            DocumentType        = input.DocumentType,
            Notes               = input.Notes,
            AttachmentRequestId = input.AttachmentRequestId,
            Status              = AttachmentStatus.Received,
            CreatedAt           = DateTime.UtcNow
        };
        db.ClaimAttachments.Add(attachment);
        await db.SaveChangesAsync();
        AddHistory(attachment.Id, null, AttachmentStatus.Received, "Attachment received by payer system");
        await db.SaveChangesAsync();

        var result = await RunCoreAsync(attachment,
            input.File.FileName, input.File.ContentType, fileSizeBytes,
            input.ProviderNPI, input.PatientName, input.ServiceDate, input.DocumentType);

        if (result.Success && input.AttachmentRequestId.HasValue)
        {
            var req = await db.AttachmentRequests.FindAsync(input.AttachmentRequestId.Value);
            if (req is not null)
                req.Status = AttachmentRequestStatus.Fulfilled;
            await db.SaveChangesAsync();
        }

        return result;
    }

    // [X12-275-CLEARINGHOUSE-PLACEHOLDER] In production, this method accepts a parsed X12 275
    // pulled from the clearinghouse rather than a pre-stored ClearinghouseAttachment record.
    public async Task<ProcessingResult> ProcessFromClearinghouseAsync(ClearinghouseAttachment ch)
    {
        var fileHash = await ComputeHashAsync(fileStorage.GetFilePath(ch.StoredFileName));

        var attachment = new ClaimAttachment
        {
            SourceType     = AttachmentSourceType.UnsolicitedClearinghouse,
            FileName       = ch.FileName,
            StoredFileName = ch.StoredFileName,
            FileSizeBytes  = ch.FileSizeBytes,
            ContentType    = ch.ContentType,
            FileHash       = fileHash,
            SubmittedBy    = $"Clearinghouse (NPI: {ch.ProviderNPI})",
            SubmittedAt    = ch.SubmittedAt,
            PatientName    = ch.PatientName,
            PatientDOB     = ch.PatientDOB,
            ProviderNPI    = ch.ProviderNPI,
            ServiceDate    = ch.ServiceDate,
            DocumentType   = ch.DocumentType,
            Status         = AttachmentStatus.Received,
            CreatedAt      = DateTime.UtcNow
        };
        db.ClaimAttachments.Add(attachment);
        await db.SaveChangesAsync();
        AddHistory(attachment.Id, null, AttachmentStatus.Received,
            $"Pulled from mock clearinghouse (tracking: {ch.TrackingNumber})");
        await db.SaveChangesAsync();

        return await RunCoreAsync(attachment,
            ch.FileName, ch.ContentType, ch.FileSizeBytes,
            ch.ProviderNPI, ch.PatientName, ch.ServiceDate, ch.DocumentType);
    }

    // Workflow 3: EMR document selected from seeded list (file already on disk in emr-samples/).
    // [X12-275-PLACEHOLDER] In production, a real EMR would transmit an X12 275 or FHIR
    // DocumentReference instead of selecting from a local file list.
    public async Task<ProcessingResult> ProcessFromEmrDocAsync(
        EmrDocument doc, string submittedBy, string? notes)
    {
        var sourcePath = fileStorage.GetEmrSamplePath(doc.FileName);
        var (storedFileName, fileSizeBytes) = await fileStorage.StoreFromPathAsync(sourcePath);
        var fileHash = await ComputeHashAsync(fileStorage.GetFilePath(storedFileName));

        var attachment = new ClaimAttachment
        {
            SourceType     = AttachmentSourceType.UnsolicitedDirect,
            FileName       = doc.FileName,
            StoredFileName = storedFileName,
            FileSizeBytes  = fileSizeBytes,
            ContentType    = "text/xml",
            FileHash       = fileHash,
            SubmittedBy    = submittedBy,
            SubmittedAt    = DateTime.UtcNow,
            PatientName    = doc.PatientName,
            PatientDOB     = doc.PatientDOB,
            ProviderNPI    = doc.ProviderNPI,
            ServiceDate    = doc.ServiceDate,
            DocumentType   = doc.DocumentType,
            Notes          = notes,
            Status         = AttachmentStatus.Received,
            CreatedAt      = DateTime.UtcNow
        };
        db.ClaimAttachments.Add(attachment);
        await db.SaveChangesAsync();
        AddHistory(attachment.Id, null, AttachmentStatus.Received,
            $"Received via direct EMR submission (doc: {doc.DocumentName})");
        await db.SaveChangesAsync();

        return await RunCoreAsync(attachment,
            doc.FileName, "text/xml", fileSizeBytes,
            doc.ProviderNPI, doc.PatientName, doc.ServiceDate, doc.DocumentType);
    }

    // Shared pipeline: validate → match → persist results → return
    private async Task<ProcessingResult> RunCoreAsync(
        ClaimAttachment attachment,
        string fileName, string contentType, long fileSizeBytes,
        string providerNPI, string patientName, DateOnly serviceDate, string documentType)
    {
        attachment.Status = AttachmentStatus.Processing;
        AddHistory(attachment.Id, AttachmentStatus.Received, AttachmentStatus.Processing,
            "Pipeline started: validating file and metadata");
        await db.SaveChangesAsync();

        var errors = new List<string>();
        var warnings = new List<string>();

        await CheckDuplicatesAsync(attachment, providerNPI, patientName, serviceDate, documentType, errors, warnings);
        Validate(fileName, contentType, fileSizeBytes, providerNPI, patientName, attachment.PatientDOB,
            serviceDate, documentType, errors, warnings);
        ValidateMagicBytes(fileStorage.GetFilePath(attachment.StoredFileName), contentType, errors);
        ValidateNpi(providerNPI, errors);

        // C-CDA structural, template, and identity validation for XML submissions
        if (contentType is "text/xml" or "application/xml")
            ValidateCcda(fileStorage.GetFilePath(attachment.StoredFileName), documentType,
                patientName, attachment.PatientDOB, providerNPI, serviceDate, errors, warnings);

        db.AttachmentValidationResults.Add(new AttachmentValidationResult
        {
            ClaimAttachmentId = attachment.Id,
            IsValid           = errors.Count == 0,
            Errors            = JsonSerializer.Serialize(errors),
            Warnings          = JsonSerializer.Serialize(warnings),
            ValidatedAt       = DateTime.UtcNow
        });

        if (errors.Count > 0)
        {
            attachment.Status = AttachmentStatus.ValidationFailed;
            AddHistory(attachment.Id, AttachmentStatus.Processing, AttachmentStatus.ValidationFailed,
                $"{errors.Count} validation error(s) found");
            await db.SaveChangesAsync();
            return new ProcessingResult(false, attachment.Id, AttachmentStatus.ValidationFailed,
                errors, warnings, null);
        }

        attachment.Status = AttachmentStatus.Validated;
        AddHistory(attachment.Id, AttachmentStatus.Processing, AttachmentStatus.Validated,
            "All validation checks passed");

        // [X12-837-LINKAGE-PLACEHOLDER] In production, use the X12 837 ICN from the original
        // claim transaction for exact matching instead of name + NPI + date proximity.
        var matchResult = await claimMatcher.MatchAsync(
            providerNPI, patientName, serviceDate, attachment.PatientDOB);
        warnings.AddRange(matchResult.Warnings);

        if (matchResult.Claim is null)
        {
            attachment.Status = AttachmentStatus.MatchFailed;
            AddHistory(attachment.Id, AttachmentStatus.Validated, AttachmentStatus.MatchFailed,
                "No claim found matching NPI + patient name + service date (±7 days)");
            await db.SaveChangesAsync();
            return new ProcessingResult(false, attachment.Id, AttachmentStatus.MatchFailed,
                errors, warnings, null);
        }

        var matchedClaim = matchResult.Claim;
        attachment.ClaimId = matchedClaim.Id;
        attachment.Status = AttachmentStatus.Matched;
        AddHistory(attachment.Id, AttachmentStatus.Validated, AttachmentStatus.Matched,
            $"Matched to claim {matchedClaim.ClaimNumber}");

        attachment.Status = AttachmentStatus.Accepted;
        AddHistory(attachment.Id, AttachmentStatus.Matched, AttachmentStatus.Accepted,
            "Attachment accepted");

        await db.SaveChangesAsync();
        return new ProcessingResult(true, attachment.Id, AttachmentStatus.Accepted,
            errors, warnings, matchedClaim);
    }

    private static async Task<string> ComputeHashAsync(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    private void AddHistory(int attachmentId, string? from, string to, string? notes)
    {
        db.AttachmentStatusHistories.Add(new AttachmentStatusHistory
        {
            ClaimAttachmentId = attachmentId,
            FromStatus        = from,
            ToStatus          = to,
            Notes             = notes,
            ChangedAt         = DateTime.UtcNow,
            ChangedBy         = "System"
        });
    }

    private static void ValidateNpi(string providerNPI, List<string> errors)
    {
        // Skip registry check if NPI already failed format validation.
        if (!NpiRegex().IsMatch(providerNPI))
            return;

        if (!MockNpiRegistry.ContainsKey(providerNPI))
            errors.Add($"Provider NPI {providerNPI} was not found in the NPI registry. " +
                       "Verify the NPI is correct and active.");
    }

    private async Task CheckDuplicatesAsync(
        ClaimAttachment attachment,
        string providerNPI, string patientName, DateOnly serviceDate, string documentType,
        List<string> errors, List<string> warnings)
    {
        // Identical file content (same SHA-256 hash) — hard error.
        var hashMatch = await db.ClaimAttachments
            .Where(a => a.FileHash == attachment.FileHash && a.Id != attachment.Id)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new { a.Id, a.SubmittedAt })
            .FirstOrDefaultAsync();

        if (hashMatch is not null)
            errors.Add($"Duplicate file: this exact document was already received " +
                       $"(Attachment #{hashMatch.Id}, submitted {hashMatch.SubmittedAt:MM/dd/yyyy HH:mm} UTC).");

        // Same provider + patient + service date + document type already accepted — warning only,
        // since a legitimately different file may cover the same encounter.
        var normalized = patientName.Trim().ToLower();
        var metaCandidates = await db.ClaimAttachments
            .Where(a => a.ProviderNPI == providerNPI
                     && a.ServiceDate == serviceDate
                     && a.DocumentType == documentType
                     && a.Status == AttachmentStatus.Accepted
                     && a.Id != attachment.Id)
            .ToListAsync();

        var metaMatch = metaCandidates.FirstOrDefault(a => a.PatientName.Trim().ToLower() == normalized);
        if (metaMatch is not null)
            warnings.Add($"Potential duplicate: an accepted {documentType} for this patient, " +
                         $"provider NPI, and service date already exists (Attachment #{metaMatch.Id}).");
    }

    private static void Validate(
        string fileName, string contentType, long fileSizeBytes,
        string providerNPI, string patientName, DateOnly? patientDOB, DateOnly serviceDate,
        string documentType, List<string> errors, List<string> warnings)
    {
        if (fileSizeBytes == 0)
            errors.Add("File is empty.");
        if (fileSizeBytes > 10 * 1024 * 1024)
            errors.Add("File size exceeds the 10 MB limit.");
        if (!AllowedMimeTypes.Contains(contentType))
            errors.Add($"File type '{contentType}' is not allowed. Accepted: PDF, JPEG, PNG, TIFF, plain text.");
        if (string.IsNullOrWhiteSpace(fileName))
            errors.Add("Filename is missing.");

        if (!NpiRegex().IsMatch(providerNPI))
            errors.Add("Provider NPI must be exactly 10 digits.");
        if (string.IsNullOrWhiteSpace(patientName))
            errors.Add("Patient name is required.");
        if (serviceDate > DateOnly.FromDateTime(DateTime.Today))
            errors.Add("Service date cannot be in the future.");
        if (serviceDate < DateOnly.FromDateTime(DateTime.Today.AddYears(-2)))
            errors.Add("Service date is more than 2 years ago (timely filing limit exceeded).");
        if (string.IsNullOrWhiteSpace(documentType))
            errors.Add("Document type is required.");

        if (patientDOB.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (patientDOB.Value > today)
                errors.Add("Patient date of birth cannot be in the future.");
            else if (patientDOB.Value < today.AddYears(-150))
                errors.Add("Patient date of birth is not plausible (more than 150 years ago).");
            if (patientDOB.Value > serviceDate)
                errors.Add("Patient date of birth cannot be after the service date.");
        }
    }

    private static void ValidateMagicBytes(string filePath, string contentType, List<string> errors)
    {
        if (contentType is "text/plain" or "text/xml" or "application/xml")
            return;

        Span<byte> header = stackalloc byte[8];
        int read;
        try
        {
            using var fs = File.OpenRead(filePath);
            read = fs.Read(header);
        }
        catch { return; }

        if (read < 4) return;

        var ok = contentType switch
        {
            "application/pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46,
            "image/jpeg"      => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png"       => read >= 8
                                 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                                 && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
            "image/tiff"      => (header[0] == 0x49 && header[1] == 0x49 && header[2] == 0x2A && header[3] == 0x00)
                              || (header[0] == 0x4D && header[1] == 0x4D && header[2] == 0x00 && header[3] == 0x2A),
            _                 => true
        };

        if (!ok)
            errors.Add($"File content does not match the declared MIME type '{contentType}'. " +
                       "The file may be corrupt or its extension mislabeled.");
    }

    private static void ValidateCcda(
        string filePath, string documentType,
        string patientName, DateOnly? patientDOB, string providerNPI, DateOnly serviceDate,
        List<string> errors, List<string> warnings)
    {
        XDocument doc;
        try { doc = XDocument.Load(filePath); }
        catch (Exception ex)
        {
            errors.Add($"XML is not well-formed: {ex.Message}");
            return;
        }

        if (doc.Root is null || doc.Root.Name.LocalName != "ClinicalDocument")
        {
            errors.Add("Document is not a CDA ClinicalDocument. Root element must be <ClinicalDocument>.");
            return;
        }

        if (doc.Root.Name.Namespace != CdaNs)
        {
            errors.Add($"ClinicalDocument is not in the HL7 CDA namespace (urn:hl7-org:v3). Found: {doc.Root.Name.Namespace}");
            return;
        }

        // Required CDA R2 header elements
        if (doc.Root.Element(CdaNs + "recordTarget") is null) errors.Add("C-CDA: missing required <recordTarget> element.");
        if (doc.Root.Element(CdaNs + "author") is null)       errors.Add("C-CDA: missing required <author> element.");
        if (doc.Root.Element(CdaNs + "custodian") is null)    errors.Add("C-CDA: missing required <custodian> element.");
        if (doc.Root.Element(CdaNs + "effectiveTime") is null) errors.Add("C-CDA: missing required <effectiveTime> element.");

        var hasUsRealmTemplate = doc.Root.Elements(CdaNs + "templateId")
            .Any(t => t.Attribute("root")?.Value == "2.16.840.1.113883.10.20.22.1.1");
        if (!hasUsRealmTemplate)
            warnings.Add("C-CDA: US Realm Header templateId (2.16.840.1.113883.10.20.22.1.1) not found. Document may not be C-CDA R2.1 compliant.");

        var codeEl = doc.Root.Element(CdaNs + "code");
        if (codeEl is null)
        {
            errors.Add("C-CDA: missing required document type <code> element (LOINC).");
            return;
        }

        var docLoinc = codeEl.Attribute("code")?.Value;
        if (string.IsNullOrEmpty(docLoinc))
        {
            errors.Add("C-CDA: <code> element is missing the LOINC 'code' attribute.");
            return;
        }

        // Cross-validate LOINC in the document against the submitted DocumentType metadata field.
        if (LoincByDocumentType.TryGetValue(documentType, out var expectedLoincs) &&
            !expectedLoincs.Contains(docLoinc))
        {
            errors.Add($"C-CDA: document type '{documentType}' expects LOINC " +
                       $"{string.Join(" or ", expectedLoincs)}, but <code> contains {docLoinc}.");
        }

        // Template conformance: check that required sections are present for this document type.
        var sectionCodes = doc.Descendants(CdaNs + "section")
            .Select(s => s.Element(CdaNs + "code")?.Attribute("code")?.Value)
            .Where(c => c is not null)
            .ToHashSet();

        if (RequiredSectionsByLoinc.TryGetValue(docLoinc, out var requiredSections))
        {
            foreach (var (code, display) in requiredSections)
            {
                if (!sectionCodes.Contains(code))
                    errors.Add($"C-CDA template: required section '{display}' (LOINC {code}) is missing.");
            }
        }

        // Section code system: each coded section should declare the LOINC OID.
        var nonLoinc = doc.Descendants(CdaNs + "section")
            .Select(s => s.Element(CdaNs + "code"))
            .Where(c => c?.Attribute("code") is not null
                     && c.Attribute("codeSystem")?.Value != "2.16.840.1.113883.6.1")
            .Select(c => c!.Attribute("code")!.Value)
            .ToList();
        if (nonLoinc.Count > 0)
            warnings.Add($"C-CDA: section code(s) [{string.Join(", ", nonLoinc)}] do not declare " +
                         "the LOINC codeSystem OID (2.16.840.1.113883.6.1).");

        // Identity cross-checks: C-CDA content vs submitted metadata
        var patientEl = doc.Root
            .Element(CdaNs + "recordTarget")?
            .Element(CdaNs + "patientRole")?
            .Element(CdaNs + "patient");

        if (patientEl is not null)
        {
            var nameEl = patientEl.Element(CdaNs + "name");
            if (nameEl is not null)
            {
                var given  = nameEl.Element(CdaNs + "given")?.Value.Trim() ?? "";
                var family = nameEl.Element(CdaNs + "family")?.Value.Trim() ?? "";
                var ccdaName = $"{given} {family}".Trim();
                if (!string.Equals(ccdaName, patientName.Trim(), StringComparison.OrdinalIgnoreCase))
                    errors.Add($"C-CDA identity: patient name in document ('{ccdaName}') does not match " +
                               $"submitted patient name ('{patientName.Trim()}').");
            }

            var birthTimeStr = patientEl.Element(CdaNs + "birthTime")?.Attribute("value")?.Value;
            if (birthTimeStr is { Length: >= 8 } &&
                DateOnly.TryParseExact(birthTimeStr[..8], "yyyyMMdd", out var ccdaDob))
            {
                if (patientDOB.HasValue && ccdaDob != patientDOB.Value)
                    errors.Add($"C-CDA identity: patient date of birth in document ({ccdaDob:MM/dd/yyyy}) " +
                               $"does not match submitted DOB ({patientDOB.Value:MM/dd/yyyy}).");
            }
        }

        var authorNpis = doc.Root.Elements(CdaNs + "author")
            .SelectMany(a => a.Element(CdaNs + "assignedAuthor")?.Elements(CdaNs + "id")
                             ?? Enumerable.Empty<XElement>())
            .Where(id => id.Attribute("root")?.Value == "2.16.840.1.113883.4.6")
            .Select(id => id.Attribute("extension")?.Value)
            .Where(n => n is not null)
            .ToHashSet();

        if (authorNpis.Count > 0 && !authorNpis.Contains(providerNPI))
            errors.Add($"C-CDA identity: author NPI in document ({string.Join(", ", authorNpis)}) " +
                       $"does not match submitted provider NPI ({providerNPI}).");

        var effectiveTimeStr = doc.Root.Element(CdaNs + "effectiveTime")?.Attribute("value")?.Value;
        if (effectiveTimeStr is { Length: >= 8 } &&
            DateOnly.TryParseExact(effectiveTimeStr[..8], "yyyyMMdd", out var ccdaEffective))
        {
            var daysDiff = Math.Abs(ccdaEffective.DayNumber - serviceDate.DayNumber);
            if (daysDiff > 30)
                warnings.Add($"C-CDA: document effective date ({ccdaEffective:MM/dd/yyyy}) differs from " +
                             $"submitted service date ({serviceDate:MM/dd/yyyy}) by {daysDiff} days.");
        }
    }

    [GeneratedRegex(@"^\d{10}$")]
    private static partial Regex NpiRegex();
}
