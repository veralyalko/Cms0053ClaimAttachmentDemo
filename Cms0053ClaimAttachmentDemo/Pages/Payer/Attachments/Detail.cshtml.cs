using System.Text.Json;
using Cms0053ClaimAttachmentDemo.Data;
using Cms0053ClaimAttachmentDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Cms0053ClaimAttachmentDemo.Pages.Payer.Attachments;

public class DetailModel(AppDbContext db) : PageModel
{
    public ClaimAttachment Attachment { get; set; } = null!;
    public List<string> ValidationErrors { get; set; } = [];
    public List<string> ValidationWarnings { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var attachment = await db.ClaimAttachments
            .Include(a => a.Claim)
            .Include(a => a.AttachmentRequest)
            .Include(a => a.ValidationResult)
            .Include(a => a.StatusHistory.OrderBy(h => h.ChangedAt))
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attachment is null)
            return NotFound();

        Attachment = attachment;

        if (attachment.ValidationResult is not null)
        {
            ValidationErrors = JsonSerializer.Deserialize<List<string>>(
                attachment.ValidationResult.Errors) ?? [];
            ValidationWarnings = JsonSerializer.Deserialize<List<string>>(
                attachment.ValidationResult.Warnings) ?? [];
        }

        return Page();
    }
}
