using Cms0053ClaimAttachmentDemo.Data;
using Cms0053ClaimAttachmentDemo.Models;
using Cms0053ClaimAttachmentDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Cms0053ClaimAttachmentDemo.Pages.Payer;

public class DashboardModel(
    AppDbContext db,
    MockClearinghouseService clearinghouse,
    AttachmentProcessingService pipeline) : PageModel
{
    public int TotalAttachments { get; set; }
    public int AcceptedCount { get; set; }
    public int FailedCount { get; set; }
    public int PendingClearinghouseCount { get; set; }
    public List<ClaimAttachment> RecentAttachments { get; set; } = [];

    [TempData]
    public string? PullMessage { get; set; }

    public async Task OnGetAsync()
    {
        TotalAttachments          = await db.ClaimAttachments.CountAsync();
        AcceptedCount             = await db.ClaimAttachments.CountAsync(a => a.Status == AttachmentStatus.Accepted);
        FailedCount               = await db.ClaimAttachments.CountAsync(a =>
                                        a.Status == AttachmentStatus.ValidationFailed ||
                                        a.Status == AttachmentStatus.MatchFailed);
        PendingClearinghouseCount = await clearinghouse.CountNewAsync();

        RecentAttachments = await db.ClaimAttachments
            .Include(a => a.Claim)
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostPullAsync()
    {
        var pulled = await clearinghouse.PullNewAttachmentsAsync();

        if (pulled.Count == 0)
        {
            PullMessage = "No new attachments found in the clearinghouse.";
            return RedirectToPage();
        }

        int accepted = 0, failed = 0;
        foreach (var ch in pulled)
        {
            var result = await pipeline.ProcessFromClearinghouseAsync(ch);
            if (result.Success) accepted++;
            else failed++;
        }

        PullMessage = $"Pulled {pulled.Count} attachment(s) from clearinghouse: " +
                      $"{accepted} accepted, {failed} failed.";

        return RedirectToPage();
    }
}
