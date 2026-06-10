using Cms0053ClaimAttachmentDemo.Data;
using Cms0053ClaimAttachmentDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Cms0053ClaimAttachmentDemo.Pages.Payer.Requests;

public class DetailModel(AppDbContext db) : PageModel
{
    public AttachmentRequest AttachReq { get; set; } = null!;
    public List<ClaimAttachment> SubmittedAttachments { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var request = await db.AttachmentRequests
            .Include(r => r.Claim)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request is null)
            return NotFound();

        AttachReq = request;

        SubmittedAttachments = await db.ClaimAttachments
            .Where(a => a.AttachmentRequestId == id)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

        return Page();
    }
}
