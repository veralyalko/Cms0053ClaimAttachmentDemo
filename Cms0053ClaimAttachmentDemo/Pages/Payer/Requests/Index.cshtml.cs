using Cms0053ClaimAttachmentDemo.Data;
using Cms0053ClaimAttachmentDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Cms0053ClaimAttachmentDemo.Pages.Payer.Requests;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<AttachmentRequest> Requests { get; set; } = [];

    public async Task OnGetAsync()
    {
        // Auto-expire pending requests that have passed their due date.
        var overdue = await db.AttachmentRequests
            .Where(r => r.Status == AttachmentRequestStatus.Pending && r.DueDate < DateTime.UtcNow)
            .ToListAsync();
        foreach (var r in overdue)
            r.Status = AttachmentRequestStatus.Expired;
        if (overdue.Count > 0)
            await db.SaveChangesAsync();

        Requests = await db.AttachmentRequests
            .Include(r => r.Claim)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
    }
}
