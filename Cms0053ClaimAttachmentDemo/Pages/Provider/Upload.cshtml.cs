using System.ComponentModel.DataAnnotations;
using Cms0053ClaimAttachmentDemo.Data;
using Cms0053ClaimAttachmentDemo.Models;
using Cms0053ClaimAttachmentDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Cms0053ClaimAttachmentDemo.Pages.Provider;

public class UploadModel(AppDbContext db, AttachmentProcessingService pipeline) : PageModel
{
    public AttachmentRequest AttachReq { get; set; } = null!;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string token)
    {
        var request = await LoadRequest(token);
        if (request is null) return NotFound();
        AttachReq = request;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string token)
    {
        var request = await LoadRequest(token);
        if (request is null) return NotFound();
        AttachReq = request;

        if (!ModelState.IsValid)
            return Page();

        // [X12-275-PLACEHOLDER] In production, this would accept an X12 275 attachment
        // transaction from the provider system instead of a form file upload.
        var processingInput = new AttachmentProcessingInput(
            File:                Input.File!,
            SourceType:          AttachmentSourceType.Solicited,
            PatientName:         request.Claim.PatientName,
            PatientDOB:          request.Claim.PatientDOB,
            ProviderNPI:         request.Claim.ProviderNPI,
            ServiceDate:         request.Claim.ServiceDate,
            DocumentType:        request.DocumentTypeRequested,
            SubmittedBy:         Input.SubmittedBy,
            Notes:               Input.Notes,
            AttachmentRequestId: request.Id
        );

        var result = await pipeline.ProcessAsync(processingInput);
        return RedirectToPage("/Payer/Attachments/Detail", new { id = result.ClaimAttachmentId });
    }

    private async Task<AttachmentRequest?> LoadRequest(string token) =>
        await db.AttachmentRequests
            .Include(r => r.Claim)
            .FirstOrDefaultAsync(r => r.SecureUploadToken == token);

    public class InputModel
    {
        [Required(ErrorMessage = "Please select a file to upload.")]
        public IFormFile? File { get; set; }

        [Required(ErrorMessage = "Please enter your name or practice name.")]
        public string SubmittedBy { get; set; } = "";

        public string? Notes { get; set; }
    }
}
