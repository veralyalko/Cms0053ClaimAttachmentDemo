using Cms0053ClaimAttachmentDemo.Data;
using Cms0053ClaimAttachmentDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Cms0053ClaimAttachmentDemo.Pages.Payer.Attachments;

public class IndexModel(AppDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SourceType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Npi { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Patient { get; set; }

    public List<ClaimAttachment> Attachments { get; set; } = [];
    public int TotalCount { get; set; }

    public static readonly string[] AllStatuses =
    [
        AttachmentStatus.Received, AttachmentStatus.Processing,
        AttachmentStatus.Validated, AttachmentStatus.ValidationFailed,
        AttachmentStatus.Matched, AttachmentStatus.MatchFailed,
        AttachmentStatus.Accepted, AttachmentStatus.Rejected
    ];

    public static readonly (string Value, string Label)[] AllSourceTypes =
    [
        (AttachmentSourceType.Solicited,                "W1 – Solicited"),
        (AttachmentSourceType.UnsolicitedClearinghouse, "W2 – Clearinghouse"),
        (AttachmentSourceType.UnsolicitedDirect,        "W3 – Direct / EMR")
    ];

    public bool HasFilters => !string.IsNullOrWhiteSpace(Status)
                           || !string.IsNullOrWhiteSpace(SourceType)
                           || !string.IsNullOrWhiteSpace(Npi)
                           || !string.IsNullOrWhiteSpace(Patient);

    public async Task OnGetAsync()
    {
        var query = db.ClaimAttachments
            .Include(a => a.Claim)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Status))
            query = query.Where(a => a.Status == Status);

        if (!string.IsNullOrWhiteSpace(SourceType))
            query = query.Where(a => a.SourceType == SourceType);

        if (!string.IsNullOrWhiteSpace(Npi))
            query = query.Where(a => a.ProviderNPI.Contains(Npi.Trim()));

        if (!string.IsNullOrWhiteSpace(Patient))
            query = query.Where(a => a.PatientName.ToLower().Contains(Patient.Trim().ToLower()));

        TotalCount = await query.CountAsync();

        Attachments = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
}
