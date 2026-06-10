using Cms0053ClaimAttachmentDemo.Data;
using Cms0053ClaimAttachmentDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace Cms0053ClaimAttachmentDemo.Services;

public class ClaimMatchingService(AppDbContext db)
{
    // [X12-837-LINKAGE-PLACEHOLDER] In production, use the X12 837 ICN (Internal Control Number)
    // from the original claim transaction for exact matching instead of name + NPI + date proximity.
    public async Task<Claim?> MatchAsync(string providerNPI, string patientName, DateOnly serviceDate)
    {
        var normalized = patientName.Trim().ToLower();

        var candidates = await db.Claims
            .Where(c => c.ProviderNPI == providerNPI)
            .ToListAsync();

        return candidates
            .Where(c => c.PatientName.Trim().ToLower() == normalized)
            .Where(c => Math.Abs(c.ServiceDate.DayNumber - serviceDate.DayNumber) <= 7)
            .OrderBy(c => Math.Abs(c.ServiceDate.DayNumber - serviceDate.DayNumber))
            .FirstOrDefault();
    }
}
