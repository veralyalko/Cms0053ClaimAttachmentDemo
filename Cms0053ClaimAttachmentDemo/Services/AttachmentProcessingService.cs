using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Cms0053ClaimAttachmentDemo.Data;
using Cms0053ClaimAttachmentDemo.Models;

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
            doc.FileName, "text/plain", fileSizeBytes,
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
        Validate(fileName, contentType, fileSizeBytes, providerNPI, patientName, serviceDate, documentType,
            errors, warnings);

        // C-CDA structural validation for XML submissions
        if (contentType is "text/xml" or "application/xml")
            ValidateCcda(fileStorage.GetFilePath(attachment.StoredFileName), errors, warnings);

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

    private static void Validate(
        string fileName, string contentType, long fileSizeBytes,
        string providerNPI, string patientName, DateOnly serviceDate, string documentType,
        List<string> errors, List<string> warnings)
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
    }

    private static void ValidateCcda(string filePath, List<string> errors, List<string> warnings)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Load(filePath);
        }
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
        if (doc.Root.Element(CdaNs + "recordTarget") is null)
            errors.Add("C-CDA: missing required <recordTarget> element.");
        if (doc.Root.Element(CdaNs + "author") is null)
            errors.Add("C-CDA: missing required <author> element.");
        if (doc.Root.Element(CdaNs + "custodian") is null)
            errors.Add("C-CDA: missing required <custodian> element.");
        if (doc.Root.Element(CdaNs + "code") is null)
            errors.Add("C-CDA: missing required document type <code> element (LOINC).");
        if (doc.Root.Element(CdaNs + "effectiveTime") is null)
            errors.Add("C-CDA: missing required <effectiveTime> element.");

        // US Realm Header template — warning only since some valid docs omit it
        var hasUsRealmTemplate = doc.Root.Elements(CdaNs + "templateId")
            .Any(t => t.Attribute("root")?.Value == "2.16.840.1.113883.10.20.22.1.1");
        if (!hasUsRealmTemplate)
            warnings.Add("C-CDA: US Realm Header templateId (2.16.840.1.113883.10.20.22.1.1) not found. Document may not be C-CDA R2.1 compliant.");
    }

    [GeneratedRegex(@"^\d{10}$")]
    private static partial Regex NpiRegex();
}
