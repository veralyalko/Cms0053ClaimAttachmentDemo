using Cms0053ClaimAttachmentDemo.Data;
using Cms0053ClaimAttachmentDemo.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Cms0053ClaimAttachmentDemo.Pages.Payer.Requests;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<AttachmentRequest> Requests { get; set; } = [];

    public async Task OnGetAsync()
    {
        Requests = await db.AttachmentRequests
            .Include(r => r.Claim)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
    }
}
