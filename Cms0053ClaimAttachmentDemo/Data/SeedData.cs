using Cms0053ClaimAttachmentDemo.Models;

namespace Cms0053ClaimAttachmentDemo.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        if (db.Claims.Any())
            return;

        var claims = new List<Claim>
        {
            new()
            {
                ClaimNumber  = "CLM-2025-0001",
                PatientName  = "John Smith",
                PatientDOB   = new DateOnly(1975, 3, 15),
                ProviderNPI  = "1234567890",
                ProviderName = "Metro Health Clinic",
                ServiceDate  = new DateOnly(2025, 10, 15),
                DiagnosisCode = "M54.5",
                AmountBilled = 350.00m,
                Status       = "Open",
                CreatedAt    = DateTime.UtcNow
            },
            new()
            {
                ClaimNumber  = "CLM-2025-0002",
                PatientName  = "Maria Garcia",
                PatientDOB   = new DateOnly(1982, 7, 22),
                ProviderNPI  = "1234567890",
                ProviderName = "Metro Health Clinic",
                ServiceDate  = new DateOnly(2025, 11, 3),
                DiagnosisCode = "J45.909",
                AmountBilled = 175.00m,
                Status       = "Pending",
                CreatedAt    = DateTime.UtcNow
            },
            new()
            {
                ClaimNumber  = "CLM-2025-0003",
                PatientName  = "Robert Johnson",
                PatientDOB   = new DateOnly(1960, 12, 1),
                ProviderNPI  = "9876543210",
                ProviderName = "Riverside Family Medicine",
                ServiceDate  = new DateOnly(2025, 9, 28),
                DiagnosisCode = "E11.9",
                AmountBilled = 520.00m,
                Status       = "Open",
                CreatedAt    = DateTime.UtcNow
            },
            new()
            {
                ClaimNumber  = "CLM-2025-0004",
                PatientName  = "Linda Chen",
                PatientDOB   = new DateOnly(1990, 5, 14),
                ProviderNPI  = "9876543210",
                ProviderName = "Riverside Family Medicine",
                ServiceDate  = new DateOnly(2025, 12, 10),
                DiagnosisCode = "K21.0",
                AmountBilled = 290.00m,
                Status       = "Pending",
                CreatedAt    = DateTime.UtcNow
            },
            new()
            {
                ClaimNumber  = "CLM-2025-0005",
                PatientName  = "James Wilson",
                PatientDOB   = new DateOnly(1955, 8, 30),
                ProviderNPI  = "5551234567",
                ProviderName = "Valley Orthopedic Group",
                ServiceDate  = new DateOnly(2025, 11, 20),
                DiagnosisCode = "M17.11",
                AmountBilled = 1250.00m,
                Status       = "Open",
                CreatedAt    = DateTime.UtcNow
            },
            new()
            {
                ClaimNumber  = "CLM-2025-0006",
                PatientName  = "Patricia Moore",
                PatientDOB   = new DateOnly(1948, 2, 17),
                ProviderNPI  = "5551234567",
                ProviderName = "Valley Orthopedic Group",
                ServiceDate  = new DateOnly(2025, 12, 5),
                DiagnosisCode = "M48.06",
                AmountBilled = 680.00m,
                Status       = "Open",
                CreatedAt    = DateTime.UtcNow
            }
        };

        db.Claims.AddRange(claims);

        // EMR documents match claims 1–5 by NPI + patient name + service date.
        // Used in Workflow 3 (Direct/EMR) to simulate selecting a document from an EHR.
        // C-CDA R2.1 XML documents — structurally valid, demo data only, no real PHI.
        var emrDocs = new List<EmrDocument>
        {
            new()
            {
                DocumentName = "Lab Results — John Smith (10/15/2025)",
                DocumentType = "Lab Results",
                PatientName  = "John Smith",
                PatientDOB   = new DateOnly(1975, 3, 15),
                ProviderNPI  = "1234567890",
                ServiceDate  = new DateOnly(2025, 10, 15),
                FileName     = "lab_results_john_smith.xml"
            },
            new()
            {
                DocumentName = "Operative Report — Robert Johnson (09/28/2025)",
                DocumentType = "Operative Report",
                PatientName  = "Robert Johnson",
                PatientDOB   = new DateOnly(1960, 12, 1),
                ProviderNPI  = "9876543210",
                ServiceDate  = new DateOnly(2025, 9, 28),
                FileName     = "operative_report_robert_johnson.xml"
            },
            new()
            {
                DocumentName = "X-Ray Report — James Wilson (11/20/2025)",
                DocumentType = "Radiology Report",
                PatientName  = "James Wilson",
                PatientDOB   = new DateOnly(1955, 8, 30),
                ProviderNPI  = "5551234567",
                ServiceDate  = new DateOnly(2025, 11, 20),
                FileName     = "xray_report_james_wilson.xml"
            },
            new()
            {
                DocumentName = "Office Visit Notes — Maria Garcia (11/03/2025)",
                DocumentType = "Office Visit Notes",
                PatientName  = "Maria Garcia",
                PatientDOB   = new DateOnly(1982, 7, 22),
                ProviderNPI  = "1234567890",
                ServiceDate  = new DateOnly(2025, 11, 3),
                FileName     = "office_visit_maria_garcia.xml"
            },
            new()
            {
                DocumentName = "Physical Therapy Notes — Linda Chen (12/10/2025)",
                DocumentType = "Physical Therapy Notes",
                PatientName  = "Linda Chen",
                PatientDOB   = new DateOnly(1990, 5, 14),
                ProviderNPI  = "9876543210",
                ServiceDate  = new DateOnly(2025, 12, 10),
                FileName     = "therapy_notes_linda_chen.xml"
            },
            new()
            {
                DocumentName = "Spine Consultation Note — Patricia Moore (12/05/2025)",
                DocumentType = "Consultation Note",
                PatientName  = "Patricia Moore",
                PatientDOB   = new DateOnly(1948, 2, 17),
                ProviderNPI  = "5551234567",
                ServiceDate  = new DateOnly(2025, 12, 5),
                FileName     = "spine_consult_patricia_moore.xml"
            }
        };

        db.EmrDocuments.AddRange(emrDocs);
        db.SaveChanges();
    }
}
