using System.ComponentModel.DataAnnotations;
using Cms0053ClaimAttachmentDemo.Data;
using Cms0053ClaimAttachmentDemo.Models;
using Cms0053ClaimAttachmentDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Cms0053ClaimAttachmentDemo.Pages.Provider;

public class DirectSubmitModel(AppDbContext db, AttachmentProcessingService pipeline) : PageModel
{
    public List<EmrDocument> EmrDocuments { get; set; } = [];

    [BindProperty]
    public EmrInputModel EmrInput { get; set; } = new();

    [BindProperty]
    public UploadInputModel UploadInput { get; set; } = new();

    public static readonly string[] DocumentTypes =
    [
        "Lab Results", "Radiology Report", "Operative Report", "Discharge Summary",
        "Office Visit Notes", "Physical Therapy Notes", "Prior Authorization", "Other"
    ];

    public async Task OnGetAsync()
    {
        EmrDocuments = await db.EmrDocuments.OrderBy(d => d.DocumentName).ToListAsync();
    }

    // [X12-275-PLACEHOLDER] In production, the EMR would transmit an X12 275 or FHIR
    // DocumentReference. Here we simulate by selecting from locally seeded documents.
    public async Task<IActionResult> OnPostEmrAsync()
    {
        // Both [BindProperty] models are bound on every post. Clear and re-validate only this tab's model.
        ModelState.Clear();
        TryValidateModel(EmrInput, nameof(EmrInput));

        EmrDocuments = await db.EmrDocuments.OrderBy(d => d.DocumentName).ToListAsync();

        if (!ModelState.IsValid)
            return Page();

        var doc = await db.EmrDocuments.FindAsync(EmrInput.EmrDocumentId);
        if (doc is null)
        {
            ModelState.AddModelError("EmrInput.EmrDocumentId", "Selected document not found.");
            return Page();
        }

        var result = await pipeline.ProcessFromEmrDocAsync(
            doc,
            submittedBy: string.IsNullOrWhiteSpace(EmrInput.SubmitterName) ? "Provider (EMR)" : EmrInput.SubmitterName,
            notes: EmrInput.Notes);

        return RedirectToPage("/Payer/Attachments/Detail", new { id = result.ClaimAttachmentId });
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        // Both [BindProperty] models are bound on every post. Clear and re-validate only this tab's model.
        ModelState.Clear();
        TryValidateModel(UploadInput, nameof(UploadInput));

        EmrDocuments = await db.EmrDocuments.OrderBy(d => d.DocumentName).ToListAsync();

        if (!ModelState.IsValid)
            return Page();

        var result = await pipeline.ProcessAsync(new AttachmentProcessingInput(
            File: UploadInput.File!,
            SourceType: AttachmentSourceType.UnsolicitedDirect,
            PatientName: UploadInput.PatientName,
            PatientDOB: UploadInput.PatientDOB,
            ProviderNPI: UploadInput.ProviderNPI,
            ServiceDate: UploadInput.ServiceDate!.Value,
            DocumentType: UploadInput.DocumentType,
            SubmittedBy: string.IsNullOrWhiteSpace(UploadInput.SubmitterName) ? "Provider (Direct)" : UploadInput.SubmitterName,
            Notes: UploadInput.Notes));

        return RedirectToPage("/Payer/Attachments/Detail", new { id = result.ClaimAttachmentId });
    }

    public class EmrInputModel
    {
        [Required(ErrorMessage = "Please select a document.")]
        public int? EmrDocumentId { get; set; }
        public string? SubmitterName { get; set; }
        public string? Notes { get; set; }
    }

    public class UploadInputModel
    {
        [Required(ErrorMessage = "Provider NPI is required.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "NPI must be exactly 10 digits.")]
        public string ProviderNPI { get; set; } = "";

        [Required(ErrorMessage = "Patient name is required.")]
        public string PatientName { get; set; } = "";

        public DateOnly? PatientDOB { get; set; }

        [Required(ErrorMessage = "Service date is required.")]
        public DateOnly? ServiceDate { get; set; }

        [Required(ErrorMessage = "Document type is required.")]
        public string DocumentType { get; set; } = "";

        [Required(ErrorMessage = "A file is required.")]
        public IFormFile? File { get; set; }

        public string? SubmitterName { get; set; }
        public string? Notes { get; set; }
    }
}
