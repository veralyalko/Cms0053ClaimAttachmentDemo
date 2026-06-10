using System.ComponentModel.DataAnnotations;
using Cms0053ClaimAttachmentDemo.Data;
using Cms0053ClaimAttachmentDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Cms0053ClaimAttachmentDemo.Pages.Payer.Requests;

public class CreateModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<Claim> Claims { get; set; } = [];

    public static readonly List<string> DocumentTypes =
    [
        "Operative Report",
        "Lab Results",
        "Radiology Report",
        "Office Visit Notes",
        "Discharge Summary",
        "Physical Therapy Notes",
        "Other"
    ];

    public async Task OnGetAsync()
    {
        Claims = await db.Claims.OrderBy(c => c.ClaimNumber).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Claims = await db.Claims.OrderBy(c => c.ClaimNumber).ToListAsync();

        if (!ModelState.IsValid)
            return Page();

        // [X12-277-PLACEHOLDER] In production, generate and transmit an X12 277 Additional
        // Information Request transaction to the provider/clearinghouse instead of saving
        // a DB record and returning a URL.
        var request = new AttachmentRequest
        {
            ClaimId               = Input.ClaimId,
            TrackingNumber        = "TRK-" + Guid.NewGuid().ToString("N")[..16].ToUpper(),
            DocumentTypeRequested = Input.DocumentTypeRequested,
            RequestReason         = Input.RequestReason,
            RequestedAt           = DateTime.UtcNow,
            DueDate               = DateTime.UtcNow.AddDays(Input.DueDays),
            ProviderEmail         = Input.ProviderEmail,
            SecureUploadToken     = Guid.NewGuid().ToString("N"),
            Status                = AttachmentRequestStatus.Pending
        };

        db.AttachmentRequests.Add(request);
        await db.SaveChangesAsync();

        return RedirectToPage("Detail", new { id = request.Id });
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Please select a claim.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a claim.")]
        public int ClaimId { get; set; }

        [Required(ErrorMessage = "Document type is required.")]
        public string DocumentTypeRequested { get; set; } = "";

        [Required(ErrorMessage = "Request reason is required.")]
        public string RequestReason { get; set; } = "";

        [Required(ErrorMessage = "Provider email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string ProviderEmail { get; set; } = "";

        [Range(1, 90, ErrorMessage = "Due days must be between 1 and 90.")]
        public int DueDays { get; set; } = 14;
    }
}
